using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>UI 平面上的通用几何计算（无状态）：节点朝向、保平面、矩形命中与矩形对齐点，
    /// 由目标箭头、出牌区、单位视图、卡牌与说明气泡共用；不持有任何状态，逐次调用零分配。</summary>
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

        /// <summary>矩形上九宫格对齐点的世界坐标：按 Pivot 出的归一化位置在矩形内插值，再由矩形自身的旋转缩放换算成世界坐标；只给点、不带偏移，抬升让位一律由调用方自己加。</summary>
        public static Vector3 AnchorPoint(RectTransform rect_, EUiAnchor anchor_) =>
            rect_.TransformPoint(rect_.rect.min + Vector2.Scale(rect_.rect.size, Pivot(anchor_)));

        /// <summary>九宫格对齐点对应的 pivot：左/中/右取 x = 0 / 0.5 / 1，上/中/下取 y = 1 / 0.5 / 0。</summary>
        public static Vector2 Pivot(EUiAnchor anchor_) => anchor_ switch
        {
            EUiAnchor.TopLeft => new Vector2(0f, 1f),
            EUiAnchor.TopCenter => new Vector2(0.5f, 1f),
            EUiAnchor.TopRight => new Vector2(1f, 1f),
            EUiAnchor.MiddleLeft => new Vector2(0f, 0.5f),
            EUiAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
            EUiAnchor.MiddleRight => new Vector2(1f, 0.5f),
            EUiAnchor.BottomLeft => new Vector2(0f, 0f),
            EUiAnchor.BottomCenter => new Vector2(0.5f, 0f),
            _ => new Vector2(1f, 0f),
        };

        #endregion
    }
}
