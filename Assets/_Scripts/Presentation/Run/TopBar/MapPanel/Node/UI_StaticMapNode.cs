using UnityEngine;
using _DreamKeep.ConfigData;
using UnityEngine.UI;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>静态图标地图节点：按节点类型换图标与描边，根矩形调到图标分辨率（描边图同步拉伸铺满）。
    /// 描边只在可走态点亮；先古之民常亮且渲染为纯黑（alpha 1）。Unknown 揭示后按 SO_MapNodeVisual 换变体。</summary>
    public sealed class UI_StaticMapNode : UI_MapNode
    {
        [Tooltip("描边层（根 Image，铺满节点）")] [SerializeField]
        private Image _outline;

        [Tooltip("图标层")] [SerializeField]
        private Image _icon;

        [Tooltip("节点视觉配置（类型→图标/描边/变体）")] [SerializeField]
        private SO_MapNodeVisual _visual;

        private RectTransform _rect;

        protected override void OnBind(MapNode node_)
        {
            if (_rect == null) _rect = (RectTransform)transform;
            EMapPointType shown = node_.PointType;
            if (!_visual.TryGetDisplayVisual(shown, node_.RevealedType, out Sprite icon, out Sprite outline))
            {
                Debug.LogError($"地图节点视觉缺 {shown} 条目", this);
                return;
            }

            _icon.sprite = icon;
            _outline.sprite = outline;
            _rect.sizeDelta = icon.rect.size; // 换图标按 sprite 分辨率调根尺寸（用户拍板）
        }

        protected override void OnStateChanged(EMapNodeState state_, bool travelable_, bool ancient_)
        {
            _outline.enabled = travelable_ || ancient_;
            _outline.color = ancient_ ? Color.black : Color.white;
        }

        protected override void WriteDim(float alpha_)
        {
            Color color = _icon.color;
            color.a = alpha_;
            _icon.color = color;
        }
    }
}
