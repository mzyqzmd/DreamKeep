using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>地图装饰连线：SetLength 按长度重设自身高度并沿线铺点链——点保持预制体尺寸不形变，
    /// 相邻点之间至少空出一个点高（两个点占三倍点高，75 像素正好放两个点）；点实例借还 UI_MapPanel 的共享池，
    /// 重复调用即全清重建；线的位置与指向由消费者摆放，本组件只管"长度 + 铺点"。</summary>
    public class UI_MapLine : MonoBehaviour
    {
        [Tooltip("铺在连线上的点预制体，其 RectTransform 高度即单点占位")] [SerializeField]
        private UI_MapDot _dotPrefab;

        private RectTransform _rect;
        private VerticalLayoutGroup _layout;
        private UI_MapPanel _panel;
        private float _dotHeight;

        private void Awake() => EnsureRefs();

        // 面板装配时是 inactive：其子树下实例化的线不跑 Awake，故由 SetLength 懒建（先例 UI_CardFace.EnsureCache）
        private void EnsureRefs()
        {
            if (_rect != null) return;
            _rect = (RectTransform)transform;
            _layout = GetComponent<VerticalLayoutGroup>();
            _dotHeight = ((RectTransform)_dotPrefab.transform).rect.height;
            _panel = GetComponentInParent<UI_MapPanel>(true); // 面板装配期是 inactive，必须连 inactive 父链一起查
            Debug.Assert(_dotHeight > 0f, "Dot 预制体高度必须大于 0，否则铺点数量无意义", this);
        }

        #region 设置线长并重铺点链

        /// <summary>按长度重设线高并重铺点链：点数 = ⌊(长度 + 点高) ÷ (2 × 点高)⌋，不足一个点的极短线只清空；
        /// 间隙 = 零头 ÷ (点数 − 1)，点数恰为 1 时不设间隙。须生成在 UI_MapPanel 子树下（点池来源）。</summary>
        public void SetLength(float length_)
        {
            EnsureRefs();
            ClearDots();
            _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, length_);
            int count = Mathf.FloorToInt((length_ + _dotHeight) / (_dotHeight * 2f));
            if (count < 1) return;
            _layout.spacing = count > 1 ? (length_ - count * _dotHeight) / (count - 1) : 0f;
            for (int i = 0; i < count; i++)
            {
                UI_MapDot dot = _panel.DotPool.Get(_dotPrefab);
                dot.transform.SetParent(_rect, false);
                dot.transform.SetSiblingIndex(i);
            }
        }

        #endregion

        // 全清：现有点逐个还池。只收激活实例——Release 只失活不换挂，池内空闲点失活后仍留在子树下，不可重复归还
        private void ClearDots()
        {
            foreach (UI_MapDot dot_ in GetComponentsInChildren<UI_MapDot>())
                _panel.DotPool.Release(dot_);
        }
    }
}
