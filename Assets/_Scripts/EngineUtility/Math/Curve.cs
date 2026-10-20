using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>二次贝塞尔曲线的通用计算（无状态）：按参数 t 取曲线上的点与当地切线，供任意沿曲线布点与朝向换算使用。</summary>
    public static class Curve
    {
        #region 对外暴露的取点与切线

        /// <summary>取二次贝塞尔曲线上参数 t 处的点：p0_ 为起点、p1_ 为控制点、p2_ 为终点。</summary>
        public static Vector3 Point(Vector3 p0_, Vector3 p1_, Vector3 p2_, float t_)
        {
            float inverse = 1f - t_;
            return inverse * inverse * p0_ + 2f * inverse * t_ * p1_ + t_ * t_ * p2_;
        }

        /// <summary>取二次贝塞尔曲线上参数 t 处的切线（未归一化的导数），供朝向换算使用。</summary>
        public static Vector3 Tangent(Vector3 p0_, Vector3 p1_, Vector3 p2_, float t_) =>
            2f * (1f - t_) * (p1_ - p0_) + 2f * t_ * (p2_ - p1_);

        #endregion
    }
}
