using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>单条事件选项：继承 UI_Button 承接点击（置灰走基类 Enabled 吞点击 + 半透明外观）；
    /// 带展示卡的选项悬停时在选项旁亮出卡面与词条气泡（Card_Display，外部驱动气泡），移开即收。</summary>
    [RequireComponent(typeof(UI_HoverActive))]
    [RequireComponent(typeof(UI_HoverScaler))]
    public sealed class UI_EventOptionItem : UI_Button, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float _fadeDuration = 0.2f;

        [Tooltip("选项名")] [SerializeField] private TMP_Text _title;

        [Tooltip("副标题：机制数值；置灰时由组换成原因文案")] [SerializeField]
        private TMP_Text _subtitle;

        [Tooltip("悬停展示卡：可空（无卡牌相关语义的选项不亮）")] [SerializeField]
        private UI_CardDisplay _cardDisplay;

        private const float DisabledAlpha = 0.4f;

        private UI_HoverActive _hoverActive;
        private UI_HoverScaler _hoverScaler;

        private CanvasGroup _canvasGroup;
        private Tween _fadeTween;
        private CardModel _card; // 本选项悬停要展示的卡（可空）

        private void Awake()
        {
            _hoverActive = GetComponent<UI_HoverActive>();
            _hoverScaler = GetComponent<UI_HoverScaler>();
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
            if (_cardDisplay) _cardDisplay.gameObject.SetActive(false); // 展示卡只随悬停亮出，初始一律收起
        }

        protected override void OnDestroy() => KillFade();

        /// <summary>绑定选项内容：置灰时副标题已被组换成原因文案，这里照写即可。</summary>
        public void Bind(string title_, string subtitle_, bool enabled_, CardModel displayCard_)
        {
            _title.text = title_;
            _subtitle.text = subtitle_;
            _card = displayCard_;
            SetEnabled(enabled_);
        }

        /// <summary>归还复位：收起悬停展示卡并清卡引用、打断淡入补间——池化复用前清掉上一轮的悬停残留。
        /// 透明度与可点状态不在这里复位，由下次 Bind 按本轮条件重写。</summary>
        public void ResetVisual()
        {
            KillFade();
            if (_cardDisplay)
            {
                _cardDisplay.HideTip();
                _cardDisplay.gameObject.SetActive(false);
            }

            _card = null;
        }

        /// <summary>错峰淡入：delay_ 秒后从透明淡到目标透明度（禁用态更暗表示置灰），重复调用杀旧重播。</summary>
        public void PlayFadeIn(float delay_)
        {
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
            _canvasGroup.alpha = 0f; // 从零起播
            float target = Enabled ? 1f : DisabledAlpha;
            UiTween.PlayAlpha(ref _fadeTween, _canvasGroup, target, _fadeDuration, delay_)
                .OnComplete(() => _fadeTween = null); // 播完即弃引用：Tween 会被回收复用，留着再 Kill 会误杀别人
        }

        /// <summary>置灰/恢复：禁用点击（基类 Enabled 接管点击）并把整件调到半透明，点选后由组统一调用。
        /// 先打断淡入补间——补间目标在启动时按当时的 Enabled 算定，迟到的写入会把透明度覆盖回 1.0。</summary>
        public void SetEnabled(bool enabled_)
        {
            KillFade();
            _hoverActive.enabled = enabled_;
            _hoverScaler.enabled = enabled_;
            Enabled = enabled_;
            _canvasGroup = UiCanvas.EnsureCanvasGroup(this);
            _canvasGroup.alpha = enabled_ ? 1f : DisabledAlpha;
        }

        // 打断淡入补间并清空引用：Kill 后 DOTween 会把该 Tween 回收给他人复用，残留引用再 Kill 会误杀别的补间
        private void KillFade()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
        }

        // 悬停亮卡：有展示卡才激活 Card_Display 并绑定卡面、弹出词条气泡
        public void OnPointerEnter(PointerEventData eventData_)
        {
            if (_card == null || !_cardDisplay) return;
            _cardDisplay.gameObject.SetActive(true);
            _cardDisplay.Bind(_card);
            _cardDisplay.ShowTip();
        }

        // 移开收卡：先收气泡再失活（失活兜底也会 Hide，双保险无害）
        public void OnPointerExit(PointerEventData eventData_)
        {
            if (_card == null || !_cardDisplay) return;
            _cardDisplay.HideTip();
            _cardDisplay.gameObject.SetActive(false);
        }
    }
}