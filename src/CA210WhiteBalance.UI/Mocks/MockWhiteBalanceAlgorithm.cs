using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CA210WhiteBalance.UI.Mocks
{
    /// <summary>
    /// 模拟白平衡算法（用于GitHub Actions编译）
    /// </summary>
    public class MockWhiteBalanceAlgorithm
    {
        private readonly ILogger<MockWhiteBalanceAlgorithm> _logger;
        private readonly Random _random = new Random();

        public event EventHandler<MockDebugProgress> ProgressUpdate;
        public event EventHandler<MockDebugResult> Complete;

        public MockWhiteBalanceAlgorithm(ILogger<MockWhiteBalanceAlgorithm> logger)
        {
            _logger = logger;
        }

        public async Task<MockDebugResult> AutoDebugAsync(MockTargetConfig config, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始模拟白平衡调试");

            var result = new MockDebugResult
            {
                StartTime = DateTime.Now,
                TargetX = config.TargetX,
                TargetY = config.TargetY,
                Tolerance = config.Tolerance
            };

            try
            {
                for (int i = 1; i <= config.MaxIterations; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Success = false;
                        result.Message = "调试已取消";
                        break;
                    }

                    await Task.Delay(100, cancellationToken);

                    var progress = new MockDebugProgress
                    {
                        Step = "调整RGB增益",
                        Status = $"正在迭代 {i}/{config.MaxIterations}",
                        DeltaX = (float)(_random.NextDouble() * config.Tolerance * 2),
                        DeltaY = (float)(_random.NextDouble() * config.Tolerance * 2),
                        Iteration = i,
                        TotalIterations = config.MaxIterations
                    };

                    ProgressUpdate?.Invoke(this, progress);

                    // 模拟在第10次迭代成功
                    if (i >= 10)
                    {
                        result.Success = true;
                        result.Iterations = i;
                        result.FinalX = config.TargetX;
                        result.FinalY = config.TargetY;
                        result.FinalDeltaX = (float)(_random.NextDouble() * config.Tolerance);
                        result.FinalDeltaY = (float)(_random.NextDouble() * config.Tolerance);
                        result.Message = "调试成功 - 色度在容差范围内";
                        break;
                    }
                }

                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Message = "调试已取消";
            }

            Complete?.Invoke(this, result);
            _logger.LogInformation("模拟调试完成: {Result}", result);
            return result;
        }
    }

    /// <summary>
    /// 模拟目标配置
    /// </summary>
    public class MockTargetConfig
    {
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float Tolerance { get; set; } = 0.005f;
        public int MaxIterations { get; set; } = 50;
    }

    /// <summary>
    /// 模拟调试进度
    /// </summary>
    public class MockDebugProgress
    {
        public string Step { get; set; }
        public string Status { get; set; }
        public float DeltaX { get; set; }
        public float DeltaY { get; set; }
        public int Iteration { get; set; }
        public int TotalIterations { get; set; }
    }

    /// <summary>
    /// 模拟调试结果
    /// </summary>
    public class MockDebugResult
    {
        public bool Success { get; set; }
        public int Iterations { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float FinalX { get; set; }
        public float FinalY { get; set; }
        public float FinalDeltaX { get; set; }
        public float FinalDeltaY { get; set; }
        public float Tolerance { get; set; }
        public string Message { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }

        public override string ToString()
        {
            if (Success)
            {
                return $"成功: {Iterations}次迭代, 最终色度({FinalX:F4}, {FinalY:F4}), 偏差(±{FinalDeltaX:F4}, ±{FinalDeltaY:F4}), 耗时{Duration.TotalSeconds:F1}秒";
            }
            else
            {
                return $"失败: {Message}";
            }
        }
    }
}
