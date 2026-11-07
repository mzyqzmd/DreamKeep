using System.Collections.Generic;
using UnityEngine;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>单段连续墨线（纯逻辑）：持有有序采样点列与一个池化折线视图；
    /// 擦除删掉半径内点并按缺口把剩余点切成多段，网格只在点列变化时重建。</summary>
    public sealed class InkStroke
    {
        private const int MaxPoints = 2048; // 单线采样点上限：防止超长笔迹无限增长

        private readonly List<Vector2> _points = new();
        private UI_MapInkLine _line;

        /// <summary>不足两点即无可视线段。</summary>
        public bool IsEmpty => _points.Count < 2;

        /// <summary>采样门限追加点；变化才重建网格，首次取用摆好视图矩形与样式。</summary>
        public void AddPoint(Vector2 localPos_, Transform parent_, MonoObjectPool<UI_MapInkLine> pool_,
            UI_MapInkLine prefab_, SO_MapInkVisual visual_)
        {
            if (_points.Count >= MaxPoints) return;
            if (_points.Count > 0 &&
                (localPos_ - _points[_points.Count - 1]).sqrMagnitude <
                visual_.SampleSpacingPx * visual_.SampleSpacingPx) return;
            _points.Add(localPos_);
            if (_line == null) _line = PrepareLine(parent_, pool_, prefab_);
            _line.SetPoints(_points, visual_.LineWidthPx, visual_.InkColor);
        }

        /// <summary>从既有连续点列重建一段（擦除切段后使用）。</summary>
        public static InkStroke FromPoints(IReadOnlyList<Vector2> points_, Transform parent_,
            MonoObjectPool<UI_MapInkLine> pool_, UI_MapInkLine prefab_, SO_MapInkVisual visual_)
        {
            InkStroke stroke = new();
            for (int i = 0; i < points_.Count; i++)
                stroke.AddPoint(points_[i], parent_, pool_, prefab_, visual_);
            return stroke;
        }

        /// <summary>擦除：半径内点删除，剩余点按原序号切成多段（短于 2 点丢弃）；
        /// 没有点被删返回 null（原段保持不动），空集合表示整段被擦光。</summary>
        public IReadOnlyList<List<Vector2>> EraseAt(Vector2 localPos_, float radiusPx_,
            MonoObjectPool<UI_MapInkLine> pool_)
        {
            float sqrRadius = radiusPx_ * radiusPx_;
            var segments = new List<List<Vector2>>();
            List<Vector2> current = null;
            bool erased = false;
            for (int i = 0; i < _points.Count; i++)
            {
                if ((_points[i] - localPos_).sqrMagnitude <= sqrRadius)
                {
                    erased = true;
                    current = null; // 缺口：断开当前连续段
                    continue;
                }

                if (current == null)
                {
                    current = new List<Vector2>();
                    segments.Add(current);
                }

                current.Add(_points[i]);
            }

            if (!erased) return null;
            for (int i = segments.Count - 1; i >= 0; i--)
                if (segments[i].Count < 2) segments.RemoveAt(i);
            Clear(pool_); // 原段整体回收，新段由控制器按点列重建
            return segments;
        }

        /// <summary>视图还池、点列清空（清空按钮与换局清空共用）。</summary>
        public void Clear(MonoObjectPool<UI_MapInkLine> pool_)
        {
            if (_line != null)
            {
                pool_.Release(_line);
                _line = null;
            }

            _points.Clear();
        }

        // 折线视图铺满墨层：与墨层同矩形同 pivot，顶点才能直接用墨层局部坐标
        private static UI_MapInkLine PrepareLine(Transform parent_, MonoObjectPool<UI_MapInkLine> pool_,
            UI_MapInkLine prefab_)
        {
            UI_MapInkLine line = pool_.Get(prefab_);
            RectTransform rect = (RectTransform)line.transform;
            rect.SetParent(parent_, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return line;
        }
    }
}
