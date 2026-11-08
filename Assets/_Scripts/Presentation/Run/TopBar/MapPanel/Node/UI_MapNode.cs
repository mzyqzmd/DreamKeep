using System;
using UnityEngine;
using UnityEngine.EventSystems;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>地图节点视图基类：四态展示（可走=悬停放大 / 已通过=全亮 / 进行中=全亮 / 不可走=压暗）与点击上报。
    /// 悬停缩放统一交给自驱的 UI_HoverScaler（不可点时整组件停用，失活自带复位）；呼吸交给 UI_HoverBreath 随可走态开关。
    /// 描边与明度由子类实现。先古之民是起点标记：不可点、不放大、不出圆环。</summary>
    [RequireComponent(typeof(UI_HoverScaler))]
    [RequireComponent(typeof(UI_HoverBreath))]
    public abstract class UI_MapNode : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("不可走节点的明度（相对原图）")] [SerializeField]
        private float _dimAlpha = 0.72f;

        /// <summary>可走节点被点击：编排层据域节点切段。</summary>
        public event Action<MapNode> Clicked;

        private MapNode Node { get; set; }

        private UI_HoverScaler _hoverScaler;
        private UI_HoverBreath _breath;
        private bool _clickable;
        private bool _highlighted;
        private float _stateAlpha = 1f; // 当前状态对应明度：高亮撤除时按它还原

        // 面板 inactive 下实例化的节点不跑 Awake：悬停器/呼吸器都惰性取，避免 SetState 拿到空
        private UI_HoverScaler HoverScaler =>
            _hoverScaler != null ? _hoverScaler : _hoverScaler = GetComponent<UI_HoverScaler>();

        private UI_HoverBreath Breath =>
            _breath != null ? _breath : _breath = GetComponent<UI_HoverBreath>();

        /// <summary>绑定域节点并按状态铺视觉：图标由子类换装，描边/压暗按状态开关。</summary>
        public void Bind(MapNode node_, EMapNodeState state_)
        {
            Node = node_;
            OnBind(node_);
            SetState(state_);
        }

        /// <summary>切换四态：可走=悬停放大可点击；先古=不放大不点击；不可走压暗。</summary>
        public void SetState(EMapNodeState state_)
        {
            bool ancient = IsAncient();
            bool travelable = state_ == EMapNodeState.Travelable && !ancient;
            _clickable = travelable;
            _stateAlpha = state_ == EMapNodeState.Untravelable ? _dimAlpha : 1f;
            WriteDim(_highlighted ? 1f : _stateAlpha); // 高亮期间重绑：保持全亮不被状态压回
            HoverScaler.enabled = travelable; // 不可点时停用：不放大也不吃指针
            Breath.SetBreath(travelable); // 呼吸只给可走节点

            OnStateChanged(state_, travelable, ancient);
        }

        public void OnPointerClick(PointerEventData eventData_)
        {
            if (eventData_.button != PointerEventData.InputButton.Left) return; // 右/中键属地图标注输入，不当成切段点击
            if (IsClickable()) Clicked?.Invoke(Node);
        }

        #region 图例悬停高亮：明度强制全亮 + 相对基准微放大，移开按状态还原

        /// <summary>节点当前展示类型："？"未揭示取 Event、已揭示取 RevealedType，其余取 PointType，与视觉换装口径一致。</summary>
        public EMapPointType ShownType => Node == null
            ? EMapPointType.None
            : Node.PointType == EMapPointType.Unknown && Node.RevealedType != EMapPointType.None
                ? Node.RevealedType
                : Node.PointType;

        /// <summary>图例悬停高亮：开＝明度提到 1 并微放大，关＝还原状态明度与基准缩放。</summary>
        public void SetHighlighted(bool on_)
        {
            if (_highlighted == on_) return;
            _highlighted = on_;
            WriteDim(on_ ? 1f : _stateAlpha);
            HoverScaler.PlayScaleRelative(on_ ? HoverScaler.HoverScale : 1f);
        }

        #endregion

        /// <summary>状态铺完后的扩展点：静态节点据此开关描边，Boss 据此换描边皮肤。</summary>
        protected virtual void OnStateChanged(EMapNodeState state_, bool travelable_, bool ancient_)
        {
        }

        protected bool IsAncient() => Node is { PointType: EMapPointType.Ancient };

        private bool IsClickable() => Node is { State: EMapNodeState.Travelable } && _clickable;

        /// <summary>子类换装：按节点类型写图标（Unknown 揭示后写变体），由 Bind 调用。</summary>
        protected virtual void OnBind(MapNode node_)
        {
        }

        /// <summary>明度呈现：不可走压暗、其余全亮，子类按自己的渲染载体实现。</summary>
        protected abstract void WriteDim(float alpha_);
    }
}