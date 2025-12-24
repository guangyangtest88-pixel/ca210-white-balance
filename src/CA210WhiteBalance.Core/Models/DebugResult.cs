using System;

namespace CA210WhiteBalance.Core.Models
{
    /// <summary>
    /// 白平衡调试结果
    /// </summary>
    public class DebugResult
    {
        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>开始时间</summary>
        public DateTime StartTime { get; set; }

        /// <summary>结束时间</summary>
        public DateTime EndTime { get; set; }

        /// <summary>目标x值</summary>
        public float TargetX { get; set; }

        /// <summary>目标y值</summary>
        public float TargetY { get; set; }

        /// <summary>容差范围</summary>
        public float Tolerance { get; set; }

        /// <summary>初始测量数据</summary>
        public CA210Data InitialData { get; set; }

        /// <summary>最终测量数据</summary>
        public CA210Data FinalData { get; set; }

        /// <summary>迭代次数</summary>
        public int Iterations { get; set; }

        /// <summary>错误信息</summary>
        public string ErrorMessage { get; set; }

        /// <summary>持续时间</summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>最终x偏差</summary>
        public float FinalDeltaX => FinalData?.DeltaX(TargetX) ?? 0;

        /// <summary>最终y偏差</summary>
        public float FinalDeltaY => FinalData?.DeltaY(TargetY) ?? 0;

        public override string ToString()
        {
            if (Success)
            {
                return $"调试成功! 迭代{Iterations}次, 耗时{Duration.TotalSeconds:F1}秒, " +
                       $"最终: x={FinalData?.Sx:F4}, y={FinalData?.Sy:F4}, " +
                       $"偏差: Δx={FinalDeltaX:F4}, Δy={FinalDeltaY:F4}";
            }
            else
            {
                return $"调试失败: {ErrorMessage}";
            }
        }
    }

    /// <summary>
    /// 调试进度信息
    /// </summary>
    public class DebugProgress
    {
        /// <summary>当前步骤</summary>
        public string Step { get; set; }

        /// <summary>状态描述</summary>
        public string Status { get; set; }

        /// <summary>当前x值</summary>
        public float CurrentX { get; set; }

        /// <summary>当前y值</summary>
        public float CurrentY { get; set; }

        /// <summary>当前x偏差</summary>
        public float DeltaX { get; set; }

        /// <summary>当前y偏差</summary>
        public float DeltaY { get; set; }

        /// <summary>RGB调整量</summary>
        public RGBCommand RGB { get; set; }

        /// <summary>当前迭代次数</summary>
        public int Iteration { get; set; }

        /// <summary>总迭代次数</summary>
        public int TotalIterations { get; set; }
    }

    /// <summary>
    /// RGB命令
    /// </summary>
    public class RGBCommand
    {
        /// <summary>红色分量 (0-255)</summary>
        public int R { get; set; } = 128;

        /// <summary>绿色分量 (0-255)</summary>
        public int G { get; set; } = 128;

        /// <summary>蓝色分量 (0-255)</summary>
        public int B { get; set; } = 128;

        public override string ToString()
        {
            return $"R={R}, G={G}, B={B}";
        }

        /// <summary>限制RGB值在有效范围</summary>
        public void Clamp(int min = 0, int max = 255)
        {
            R = Math.Max(min, Math.Min(max, R));
            G = Math.Max(min, Math.Min(max, G));
            B = Math.Max(min, Math.Min(max, B));
        }
    }
}
