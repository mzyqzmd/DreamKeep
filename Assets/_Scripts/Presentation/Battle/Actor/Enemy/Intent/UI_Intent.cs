using _DreamKeep.RuntimeData;
using _DreamKeep.CommonPresentation;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>意图图标基类：悬停说明交给同节点的气泡发起器（内容在这里喂给它，触发方式与定位都在那份组件上配）；
    /// 具体图标在 Bind 里登记自己对应的意图，说明文案取它的名称与描述。</summary>
    [RequireComponent(typeof(UI_HoverTip))]
    public abstract class UI_Intent : MonoBehaviour
    {
        private UI_HoverTip _hoverTip;

        // 同节点的气泡发起器由 RequireComponent 保证存在，取出即用（预制体零额外接线）
        private void Awake() => _hoverTip = GetComponent<UI_HoverTip>();

        /// <summary>登记本图标对应的意图：具体图标的 Bind 里先调它，说明内容随即喂给同节点的气泡发起器。</summary>
        protected void BindIntent(EnemyIntent intent_) =>
            _hoverTip.SetContents(intent_.GetIntentName(), intent_.GetDescription());
    }
}