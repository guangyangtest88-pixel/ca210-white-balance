namespace CA210WhiteBalance.Core.Models
{
    /// <summary>
    /// 白平衡目标配置
    /// </summary>
    public class TargetConfig
    {
        /// <summary>目标色度x值</summary>
        public float TargetX { get; set; } = 0.3130f;

        /// <summary>目标色度y值</summary>
        public float TargetY { get; set; } = 0.3290f;

        /// <summary>目标亮度值 (可选)</summary>
        public float? TargetLv { get; set; }

        /// <summary>容差范围，默认±0.005</summary>
        public float Tolerance { get; set; } = 0.005f;

        /// <summary>最大迭代次数</summary>
        public int MaxIterations { get; set; } = 50;

        /// <summary>测量间隔(ms)</summary>
        public int MeasureInterval { get; set; } = 500;

        /// <summary>RGB调整步长</summary>
        public int RGBStep { get; set; } = 5;

        /// <summary>RGB调整范围</summary>
        public int RGBMaxAdjust { get; set; } = 50;

        public override string ToString()
        {
            return $"目标: x={TargetX:F4}, y={TargetY:F4}, 容差=±{Tolerance:F4}";
        }
    }
}
