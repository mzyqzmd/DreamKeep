using UnityEngine;

namespace _DreamKeep.Utility
{
    /// <summary>UI 平面上的通用几何计算（无状态）：节点朝向、保平面、矩形命中与单节点锚点，
    /// 由目标箭头、出牌区、单位视图与说明气泡共用；不持有任何状态，逐次调用零分配。</summary>
    public static class UiGeometry
    {
        private const float Epsilon = 0.0001f;

        #region 对外暴露的计算

        /// <summary>把节点的"上"对齐给定方向（如箭头段与头部一律朝上）；方向退化时返回单位旋转，保持原朝向。</summary>
        public static Quaternion UpRotation(Vector3 direction_) =>
            direction_.sqrMagnitude < Epsilon * Epsilon
                ? Quaternion.identity
                : Quaternion.FromToRotation(Vector3.up, direction_.normalized);

        /// <summary>保平面：只取世界点的 x/y，深度沿用给定 z_，避免把 UI 推出所在画布平面。</summary>
        public static Vector3 KeepPlane(Vector3 world_, float z_) => new Vector3(world_.x, world_.y, z_);

        /// <summary>给定世界点是否落在该矩形内：落点判定与单位命中判定共用同一口径。</summary>
        public static bool Contains(RectTransform rect_, Vector3 world_) =>
            rect_.rect.Contains(rect_.InverseTransformPoint(world_));

        /// <summary>单节点锚点：目标矩形顶沿中点的世界坐标，再沿本节点 up 抬 liftPx_（画布像素按 lossyScale 换算成世界距离）；供说明气泡定位到槽位上方。</summary>
        public static Vector3 TopCenter(Component source_, float liftPx_)
        {
            RectTransform rect = (RectTransform)source_.transform;
            return rect.TransformPoint(new Vector2(rect.rect.center.x, rect.rect.yMax)) + rect.up * (liftPx_ * rect.lossyScale.y);
        }

        #endregion
    }
}
