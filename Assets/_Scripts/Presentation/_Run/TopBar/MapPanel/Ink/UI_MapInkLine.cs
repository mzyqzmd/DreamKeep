using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>单段连续墨线视图：自定义 UGUI 网格，按点列生成法线偏移三角带 + 两端半圆圆头。
    /// 挂在墨层下（与墨层同矩形同 pivot），顶点直接用墨层局部坐标，随内容滚动、服从 UGUI 裁剪；
    /// 不吃射线，网格只在点列变化时重建。</summary>
    public sealed class UI_MapInkLine : MaskableGraphic
    {
        private const int CapSegments = 8; // 半圆帽细分段数

        private readonly List<Vector2> _points = new();
        private float _widthPx;
        private Color _inkColor = Color.white;

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false; // 笔迹不吃指针：节点点击、滚轮转发、背景关闭照常
        }

        /// <summary>写入点列与样式：仅此入口使网格失效，Update 内零网格工作。</summary>
        public void SetPoints(IReadOnlyList<Vector2> points_, float widthPx_, Color color_)
        {
            _points.Clear();
            for (int i = 0; i < points_.Count; i++) _points.Add(points_[i]);
            _widthPx = widthPx_;
            _inkColor = color_;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh_)
        {
            vh_.Clear();
            int count = _points.Count;
            if (count < 2 || _widthPx <= 0f) return;
            float half = _widthPx * 0.5f;

            // 逐点法线：相邻段法线平均 + miter 补偿，共享顶点避免拐角裂缝
            var normals = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Vector2 dirPrev = i > 0 ? DirAt(i - 1) : DirAt(0);
                Vector2 dirNext = i < count - 1 ? DirAt(i) : dirPrev;
                Vector2 nPrev = new(-dirPrev.y, dirPrev.x);
                Vector2 nNext = new(-dirNext.y, dirNext.x);
                Vector2 sum = nPrev + nNext;
                if (sum.sqrMagnitude < 1e-6f) sum = nNext;
                sum.Normalize();
                normals[i] = sum * (half / Mathf.Max(0.5f, Vector2.Dot(sum, nNext)));
            }

            for (int i = 0; i < count; i++)
            {
                vh_.AddVert(VertexAt(_points[i] + normals[i]));
                vh_.AddVert(VertexAt(_points[i] - normals[i]));
            }

            for (int i = 0; i < count - 1; i++)
            {
                int a = i * 2;
                vh_.AddTriangle(a, a + 2, a + 1);
                vh_.AddTriangle(a + 1, a + 2, a + 3);
            }

            AddCap(vh_, _points[0], -DirAt(0), half);
            AddCap(vh_, _points[count - 1], DirAt(count - 2), half);
        }

        private Vector2 DirAt(int segment_)
        {
            Vector2 d = _points[segment_ + 1] - _points[segment_];
            return d.sqrMagnitude < 1e-8f ? Vector2.right : d.normalized;
        }

        private UIVertex VertexAt(Vector2 pos_)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(pos_.x, pos_.y, 0f);
            vertex.color = _inkColor;
            return vertex;
        }

        // 半圆帽：从 +侧法线 绕朝外方向到 -侧法线；UI/Default 双面渲染，绕序不影响可见性
        private void AddCap(VertexHelper vh_, Vector2 center_, Vector2 forward_, float half_)
        {
            Vector2 side = new(-forward_.y, forward_.x);
            int centerIndex = vh_.currentVertCount;
            vh_.AddVert(VertexAt(center_));
            for (int i = 0; i <= CapSegments; i++)
            {
                float angle = Mathf.PI * i / CapSegments;
                Vector2 offset = side * Mathf.Cos(angle) + forward_ * Mathf.Sin(angle);
                vh_.AddVert(VertexAt(center_ + offset * half_));
                if (i > 0) vh_.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
            }
        }
    }
}
