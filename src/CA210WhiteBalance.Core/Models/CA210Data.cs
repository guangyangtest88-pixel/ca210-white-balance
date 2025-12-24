using System;

namespace CA210WhiteBalance.Core.Models
{
    /// <summary>
    /// CA-210色彩分析仪测量数据模型
    /// </summary>
    public class CA210Data
    {
        /// <summary>测量时间戳</summary>
        public DateTime Timestamp { get; set; }

        // 亮度与色度
        /// <summary>亮度值 (cd/m²)</summary>
        public float Lv { get; set; }

        /// <summary>色度坐标 x</summary>
        public float Sx { get; set; }

        /// <summary>色度坐标 y</summary>
        public float Sy { get; set; }

        // 色温
        /// <summary>相关色温 (K)</summary>
        public float T { get; set; }

        // uv坐标
        /// <summary>Duv值</summary>
        public float Duv { get; set; }

        /// <summary>u' 坐标</summary>
        public float Ud { get; set; }

        /// <summary>v' 坐标</summary>
        public float Vd { get; set; }

        // 三刺激值
        /// <summary>X三刺激值</summary>
        public float X { get; set; }

        /// <summary>Y三刺激值</summary>
        public float Y { get; set; }

        /// <summary>Z三刺激值</summary>
        public float Z { get; set; }

        /// <summary>计算与目标x值的偏差</summary>
        public float DeltaX(float targetX) => Math.Abs(Sx - targetX);

        /// <summary>计算与目标y值的偏差</summary>
        public float DeltaY(float targetY) => Math.Abs(Sy - targetY);

        /// <summary>检查是否在容差范围内</summary>
        /// <param name="targetX">目标x值</param>
        /// <param name="targetY">目标y值</param>
        /// <param name="tolerance">容差范围，默认0.005</param>
        public bool IsInTolerance(float targetX, float targetY, float tolerance = 0.005f)
        {
            return DeltaX(targetX) <= tolerance && DeltaY(targetY) <= tolerance;
        }

        public override string ToString()
        {
            return $"Lv={Lv:F2}, x={Sx:F4}, y={Sy:F4}, T={T:F0}K";
        }
    }
}
