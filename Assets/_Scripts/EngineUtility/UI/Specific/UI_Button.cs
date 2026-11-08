using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.EngineUtility
{
    /// <summary>可点击 UI 单元的抽象基类：自带点击上报（event Clicked）。
    /// 通过 IPointerClickHandler 接收点击，不依赖 Button 组件——同节点挂 Button 仍可复用其视觉态，
    /// 纯图片/文本单元也能直接点。具体按钮 UI 继承本类即可。</summary>
    public abstract class UI_Button : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>点击上报：组的接线、单件的订阅都收这里。</summary>
        public event Action Clicked;

        /// <summary>是否可点击：false 时吞掉点击（不触发 Clicked）；
        /// 悬停由具体子类自行广播，文案照常显示——「置灰但悬停可看、点击无作用」走这里。</summary>
        protected bool Enabled { get; set; } = true;

        /// <summary>可点击态对外出口：点击音拦截器据此判断该不该响，不改变本类的吞点击语义。</summary>
        public bool Interactable => Enabled;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData_)
        {
            if (!Enabled) return;
            Clicked?.Invoke();
        }

        // 子类清理自有补间/订阅的挂点（如淡入 Tween 的 Kill）
        protected virtual void OnDestroy()
        {
        }
    }
}