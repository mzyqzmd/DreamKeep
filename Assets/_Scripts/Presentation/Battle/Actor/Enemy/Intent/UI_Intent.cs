using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation.Intent
{
    /// <summary>意图图标基类：每个图标自己承接悬停，在自身顶沿上方弹一条说明（意图只有描述、无标题）；
    /// 具体图标在 Bind 里登记自己对应的意图，说明文案取它的 GetDescription()；
    /// 生成方 UI_EnemyIntentGroup 在生成后注入气泡宿主。</summary>
    public abstract class UI_Intent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("气泡底沿与本图标顶沿的间距（画布像素）")] [SerializeField]
        private float _tipLiftPx = 1f;

        private UI_PopTextRoot _tipHost;
        private EnemyIntent _intent;
        private bool _tipShown;

        /// <summary>装配：注入说明气泡宿主（由生成方在 Instantiate 后调用）。</summary>
        public void Initialize(UI_PopTextRoot tipHost_) => _tipHost = tipHost_;

        /// <summary>登记本图标对应的意图：具体图标的 Bind 里先调它，悬停时据此取说明文案。</summary>
        protected void BindIntent(EnemyIntent intent_) => _intent = intent_;

        public void OnPointerEnter(PointerEventData eventData_)
        {
            if (_tipHost == null || _intent == null) return;
            _tipShown = true;
            _tipHost.ShowTip(this, null, _intent.GetDescription(), UiGeometry.TopCenter(this, _tipLiftPx));
        }

        public void OnPointerExit(PointerEventData eventData_) => HideTip();

        private void OnDisable() => HideTip(); // 意图刷新重建/敌人销毁/面板关激活时 Exit 不来，兜底收掉悬着的气泡

        private void HideTip()
        {
            if (!_tipShown) return;
            _tipShown = false;
            if (_tipHost) _tipHost.HideTip(this);
        }
    }
}
