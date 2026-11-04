using System;
using UnityEngine;
using UnityEngine.EventSystems;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;

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
            WriteDim(state_ == EMapNodeState.Untravelable ? _dimAlpha : 1f);
            HoverScaler.enabled = travelable; // 不可点时停用：不放大也不吃指针
            Breath.SetBreath(travelable); // 呼吸只给可走节点

            OnStateChanged(state_, travelable, ancient);
        }

        public void OnPointerClick(PointerEventData eventData_)
        {
            if (IsClickable()) Clicked?.Invoke(Node);
        }

        /// <summary>状态铺完后的扩展点：静态节点据此开关描边，Boss 据此换描边皮肤。</summary>
        protected virtual void OnStateChanged(EMapNodeState state_, bool travelable_, bool ancient_) { }

        protected bool IsAncient() => Node != null && Node.PointType == EMapPointType.Ancient;

        private bool IsClickable() => Node != null && Node.State == EMapNodeState.Travelable && _clickable;

        /// <summary>子类换装：按节点类型写图标（Unknown 揭示后写变体），由 Bind 调用。</summary>
        protected virtual void OnBind(MapNode node_) { }

        /// <summary>明度呈现：不可走压暗、其余全亮，子类按自己的渲染载体实现。</summary>
        protected abstract void WriteDim(float alpha_);
    }
}
