using System;
using System.Threading;
using System.Threading.Tasks;
using CA210WhiteBalance.Core.CA210;
using CA210WhiteBalance.Core.Models;
using CA210WhiteBalance.Core.SerialPort;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.Core.Algorithm
{
    /// <summary>
    /// 白平衡算法接口
    /// </summary>
    public interface IWhiteBalanceAlgorithm
    {
        /// <summary>进度更新事件</summary>
        event EventHandler<DebugProgress> ProgressUpdate;

        /// <summary>完成事件</summary>
        event EventHandler<DebugResult> Complete;

        /// <summary>自动白平衡调试</summary>
        Task<DebugResult> AutoDebugAsync(TargetConfig config, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 白平衡算法实现
    /// </summary>
    public class WhiteBalanceAlgorithm : IWhiteBalanceAlgorithm
    {
        private readonly ICA210Service _ca210Service;
        private readonly ISerialPortService _serialPortService;
        private readonly ProtocolManager _protocolManager;
        private readonly ILogger<WhiteBalanceAlgorithm> _logger;

        public WhiteBalanceAlgorithm(
            ICA210Service ca210Service,
            ISerialPortService serialPortService,
            ProtocolManager protocolManager,
            ILogger<WhiteBalanceAlgorithm> logger)
        {
            _ca210Service = ca210Service;
            _serialPortService = serialPortService;
            _protocolManager = protocolManager;
            _logger = logger;
        }

        public event EventHandler<DebugProgress> ProgressUpdate;
        public event EventHandler<DebugResult> Complete;

        public async Task<DebugResult> AutoDebugAsync(
            TargetConfig config,
            CancellationToken cancellationToken = default)
        {
            var result = new DebugResult
            {
                StartTime = DateTime.Now,
                TargetX = config.TargetX,
                TargetY = config.TargetY,
                Tolerance = config.Tolerance
            };

            try
            {
                _logger.LogInformation("开始白平衡调试, 目标: x={TargetX:F4}, y={TargetY:F4}",
                    config.TargetX, config.TargetY);

                // 步骤1: 零校准
                NotifyProgress(new DebugProgress
                {
                    Step = "零校准",
                    Status = "正在执行...",
                    Iteration = 1,
                    TotalIterations = config.MaxIterations
                });

                bool calibrateSuccess = await _ca210Service.CalibrateZeroAsync();
                if (!calibrateSuccess)
                {
                    result.Success = false;
                    result.ErrorMessage = "零校准失败";
                    Complete?.Invoke(this, result);
                    return result;
                }

                // 步骤2: 初始测量
                CA210Data currentData = await _ca210Service.MeasureAsync();
                if (currentData == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "初始测量失败";
                    Complete?.Invoke(this, result);
                    return result;
                }

                result.InitialData = currentData;

                _logger.LogInformation("初始测量: x={X:F4}, y={Y:F4}",
                    currentData.Sx, currentData.Sy);

                // 初始化RGB (从基准值开始)
                RGBCommand currentRGB = new RGBCommand
                {
                    R = 128,
                    G = 128,
                    B = 128
                };

                // 步骤3: 迭代调整
                int iterations = 0;
                float prevDeltaX = float.MaxValue;
                float prevDeltaY = float.MaxValue;
                int noImprovementCount = 0;

                while (!cancellationToken.IsCancellationRequested &&
                       iterations < config.MaxIterations)
                {
                    iterations++;

                    // 步骤4: 检查是否在容差范围内
                    float deltaX = currentData.DeltaX(config.TargetX);
                    float deltaY = currentData.DeltaY(config.TargetY);

                    _logger.LogDebug("迭代 {Iter}: x={X:F4}, y={Y:F4}, Δx={DX:F4}, Δy={DY:F4}",
                        iterations, currentData.Sx, currentData.Sy, deltaX, deltaY);

                    if (currentData.IsInTolerance(config.TargetX, config.TargetY, config.Tolerance))
                    {
                        result.Success = true;
                        result.FinalData = currentData;
                        result.Iterations = iterations;
                        result.EndTime = DateTime.Now;

                        NotifyProgress(new DebugProgress
                        {
                            Step = "完成",
                            Status = $"收敛! 迭代次数: {iterations}, 耗时: {result.Duration.TotalSeconds:F1}秒",
                            CurrentX = currentData.Sx,
                            CurrentY = currentData.Sy,
                            DeltaX = deltaX,
                            DeltaY = deltaY,
                            Iteration = iterations,
                            TotalIterations = config.MaxIterations
                        });

                        _logger.LogInformation("调试成功! 迭代次数: {Iter}, 最终: x={X:F4}, y={Y:F4}",
                            iterations, currentData.Sx, currentData.Sy);

                        Complete?.Invoke(this, result);
                        return result;
                    }

                    // 检查是否有改善
                    if (deltaX >= prevDeltaX && deltaY >= prevDeltaY)
                    {
                        noImprovementCount++;
                        if (noImprovementCount > 3)
                        {
                            _logger.LogWarning("连续多次无改善，可能已陷入局部最优");
                        }
                    }
                    else
                    {
                        noImprovementCount = 0;
                    }
                    prevDeltaX = deltaX;
                    prevDeltaY = deltaY;

                    // 步骤5: 计算RGB调整量
                    RGBCommand rgbAdjust = CalculateRGBAdjustment(
                        currentData.Sx, currentData.Sy,
                        config.TargetX, config.TargetY,
                        config.RGBStep, config.RGBMaxAdjust);

                    // 应用调整到当前值
                    currentRGB.R = Clamp(currentRGB.R + rgbAdjust.R, 0, 255);
                    currentRGB.G = Clamp(currentRGB.G + rgbAdjust.G, 0, 255);
                    currentRGB.B = Clamp(currentRGB.B + rgbAdjust.B, 0, 255);

                    // 步骤6: 发送RGB指令
                    byte[] command = _protocolManager.BuildCommand(currentRGB);
                    bool sendSuccess = await _serialPortService.SendAsync(command);

                    if (!sendSuccess)
                    {
                        result.Success = false;
                        result.ErrorMessage = "RGB指令发送失败";
                        Complete?.Invoke(this, result);
                        return result;
                    }

                    // 步骤7: 等待响应
                    await Task.Delay(_protocolManager.GetResponseDelay(), cancellationToken);

                    // 步骤8: 重新测量
                    currentData = await _ca210Service.MeasureAsync();
                    if (currentData == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"第{iterations}次测量失败";
                        Complete?.Invoke(this, result);
                        return result;
                    }

                    // 更新进度
                    NotifyProgress(new DebugProgress
                    {
                        Step = $"迭代 {iterations}/{config.MaxIterations}",
                        Status = $"Δx={deltaX:F4}, Δy={deltaY:F4}",
                        CurrentX = currentData.Sx,
                        CurrentY = currentData.Sy,
                        DeltaX = deltaX,
                        DeltaY = deltaY,
                        RGB = currentRGB,
                        Iteration = iterations,
                        TotalIterations = config.MaxIterations
                    });
                }

                // 达到最大迭代次数
                result.Success = false;
                result.FinalData = currentData;
                result.Iterations = iterations;
                result.ErrorMessage = $"达到最大迭代次数({config.MaxIterations})，未收敛";

                _logger.LogWarning("调试未收敛: {Error}", result.ErrorMessage);

                Complete?.Invoke(this, result);
                return result;
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.ErrorMessage = "用户取消";
                result.EndTime = DateTime.Now;

                _logger.LogInformation("调试已取消");

                Complete?.Invoke(this, result);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;

                _logger.LogError(ex, "调试过程中发生错误");

                Complete?.Invoke(this, result);
                return result;
            }
        }

        /// <summary>
        /// 计算RGB调整量
        /// 基于xy偏差的简化算法，实际应用中可能需要根据显示设备特性调整
        /// </summary>
        private RGBCommand CalculateRGBAdjustment(
            float currentX, float currentY,
            float targetX, float targetY,
            int step, int maxAdjust)
        {
            float deltaX = targetX - currentX;
            float deltaY = targetY - currentY;

            // 色度坐标与RGB的关系 (简化版)
            // x偏红，y偏绿和蓝的混合
            // 实际应用中需要根据显示设备的光谱特性进行校准

            int rAdjust = 0;
            int gAdjust = 0;
            int bAdjust = 0;

            // x方向：增加红色分量可以提高x值
            rAdjust = (int)(deltaX * 10000 / step) * step;

            // y方向：增加绿色分量主要影响y，蓝色分量对y有反向影响
            if (deltaY > 0)
            {
                gAdjust = (int)(deltaY * 8000 / step) * step;
                // y过高时，适当增加蓝色
                bAdjust = -(int)(deltaY * 2000 / step) * step;
            }
            else
            {
                gAdjust = (int)(deltaY * 6000 / step) * step;
                bAdjust = -(int)(deltaY * 4000 / step) * step;
            }

            // 限制调整范围
            rAdjust = Clamp(rAdjust, -maxAdjust, maxAdjust);
            gAdjust = Clamp(gAdjust, -maxAdjust, maxAdjust);
            bAdjust = Clamp(bAdjust, -maxAdjust, maxAdjust);

            _logger.LogDebug("RGB调整计算: Δx={DX:F4}, Δy={DY:F4} => R:{RA}, G:{GA}, B:{BA}",
                deltaX, deltaY, rAdjust, gAdjust, bAdjust);

            return new RGBCommand
            {
                R = rAdjust,
                G = gAdjust,
                B = bAdjust
            };
        }

        private int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        private void NotifyProgress(DebugProgress progress)
        {
            ProgressUpdate?.Invoke(this, progress);
        }
    }
}
