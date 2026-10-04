using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.Utility
{
    /// <summary>UI 平面上的通用几何计算（无状态）：节点朝向、保平面、矩形命中与一组矩形的包围盒锚点，
    /// 由目标箭头、出牌区、单位视图与说明气泡容器共用；不持有任何状态，逐次调用零分配（包围盒的角点数组每次调用一份）。</summary>
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

        /// <summary>一组矩形（组件自身即 RectTransform）的世界包围盒顶沿中点，再沿世界 y 抬 liftWorld_；供说明气泡定位锚点。</summary>
        public static Vector3 BoundingTopCenter(IReadOnlyList<Component> items_, float liftWorld_)
        {
            Vector3[] corners = new Vector3[4];
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            for (int i = 0; i < items_.Count; i++)
            {
                ((RectTransform)items_[i].transform).GetWorldCorners(corners);
                for (int j = 0; j < corners.Length; j++)
                {
                    min = Vector3.Min(min, corners[j]);
                    max = Vector3.Max(max, corners[j]);
                }
            }

            return new Vector3((min.x + max.x) * 0.5f, max.y + liftWorld_, (min.z + max.z) * 0.5f);
        }

        #endregion
    }
}
