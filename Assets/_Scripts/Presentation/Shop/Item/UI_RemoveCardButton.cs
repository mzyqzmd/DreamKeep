using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>商店的删牌服务位：显示当前价、买不起染红，点击走子节点 Icon 上的 Button（该按钮实现了点击接口会截断冒泡）。
    /// 指针进出向上报，由地毯驱动商人手指过来——本店用尽后不再上报进入。
    /// 进场时把翻转动画冻结在首帧；玩家在删牌弹板按下确认的那一刻播一次并定格末帧——末帧的样子就是"不可用"，
    /// 故不额外染灰。用尽后价格隐藏、点击停止、悬停缩放停用，连说明气泡也一并抑制。</summary>
    public sealed class UI_RemoveCardButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("子节点 Icon 上的按钮：点击由它上报")] [SerializeField]
        private Button _button;

        [Tooltip("价格容器：用尽后整块隐藏")] [SerializeField]
        private GameObject _priceRoot;

        [Tooltip("价格文本")] [SerializeField] private TextMeshProUGUI _priceText;

        [SerializeField] private Color _affordableColor = Color.white;
        [SerializeField] private Color _unaffordableColor = new(1f, 0.42f, 0.42f);

        [Header("用尽演出")]
        [Tooltip("子节点 Icon 上的 Animator")] [SerializeField]
        private Animator _animator;

        [Tooltip("翻转状态名：进场冻结在它的首帧，确认时播一次")] [SerializeField]
        private string _flipState = "RemoveButtonFlip";

        [Tooltip("同节点的悬停缩放：用尽后停用")] [SerializeField]
        private UI_HoverScaler _hoverScaler;

        [Header("被拒反馈")]
        [Tooltip("抖动幅度（画布像素）")] [SerializeField]
        private float _shakeAmplitudePx = 12f;

        [Tooltip("抖动时长（秒）")] [SerializeField] private float _shakeDuration = 0.3f;

        private Tween _shakeTween;
        private bool _used;
        private bool _waitingFlipEnd;

        /// <summary>点击上报：段入口据此校验金币并打开删牌弹板。</summary>
        public event Action Clicked;

        /// <summary>指针进入：地毯据此让商人手移过来（本店已用尽就不报）。</summary>
        public event Action<UI_RemoveCardButton> Hovered;

        /// <summary>指针离开：无论是否用尽都报，防"悬停中被用尽"把商人手留在这一格。</summary>
        public event Action<UI_RemoveCardButton> Unhovered;

        private void Awake()
        {
            if (!_button) _button = GetComponentInChildren<Button>(true);
            if (!_hoverScaler) _hoverScaler = GetComponent<UI_HoverScaler>();
            _button.onClick.AddListener(OnButtonClicked);
            FreezeFlipAtFirstFrame();
        }

        // 用尽态的定格会被"失活再激活"重置（Animator 重初始化会回放默认状态首帧）：
        // 重激时按用尽态重新定格到末帧——末帧的样子就是"不可用"，不重做就会回复原样
        private void OnEnable()
        {
            if (!_used || !_animator) return;
            _animator.enabled = true;
            _animator.speed = 0f;
            _animator.Play(_flipState, 0, 1f);
            _animator.Update(0f);
        }

        // 翻转播完就定格：状态机没配"播完即停"，这里在播到末帧时把速度归零
        private void Update()
        {
            if (!_waitingFlipEnd || !_animator) return;
            if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f) return;
            _animator.speed = 0f;
            _waitingFlipEnd = false;
        }

        // 失活先停抖：在抖的先跳回基准再清（Settle），半途位移不会带进下次
        private void OnDisable() => UiTween.Settle(ref _shakeTween);

        private void OnDestroy()
        {
            if (_button) _button.onClick.RemoveListener(OnButtonClicked);
        }

        #region 对外：报价、用尽演出与被拒

        /// <summary>刷新价格与可用性：买不起染红（仍可点，点了抖动提示），用尽后价格隐藏且不再响应。</summary>
        public void ShowPrice(int price_, int gold_, bool used_)
        {
            if (_priceRoot) _priceRoot.SetActive(!used_);
            if (_priceText)
            {
                _priceText.text = price_.ToString();
                _priceText.color = gold_ >= price_ ? _affordableColor : _unaffordableColor;
            }

            if (!used_ || _used) return;
            MarkUsed();
        }

        /// <summary>玩家在删牌弹板按下确认的同一刻调用：播一次翻面，同时进入不可用态。</summary>
        public void PlayFlip()
        {
            if (_used) return;
            MarkUsed();
            if (!_animator) return;
            _animator.enabled = true;
            _animator.speed = 1f;
            _animator.Play(_flipState, 0, 0f);
            _waitingFlipEnd = true;
        }

        /// <summary>买不起时被拒：抖一下，不弹删牌面板。</summary>
        public void PlayRefuse()
        {
            if (_used) return;
            UiTween.PlayShake(ref _shakeTween, (RectTransform)transform, _shakeAmplitudePx, _shakeDuration);
        }

        #endregion

        // 不可用态：价格收起、点击关掉、悬停缩放停用（自驱组件失活时会把缩放写回基准）、说明气泡一并抑制
        private void MarkUsed()
        {
            _used = true;
            if (_priceRoot) _priceRoot.SetActive(false);
            if (_button) _button.interactable = false;
            if (_hoverScaler) _hoverScaler.enabled = false;
            if (TryGetComponent(out UI_HoverTip tip)) tip.SetSuppressed(true);
        }

        // 冻结首帧：先求值一次再关掉组件，避免默认状态一激活就自动播
        private void FreezeFlipAtFirstFrame()
        {
            if (!_animator) return;
            _animator.Play(_flipState, 0, 0f);
            _animator.Update(0f);
            _animator.enabled = false;
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (_used) return;
            Hovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData _) => Unhovered?.Invoke(this);

        private void OnButtonClicked()
        {
            if (_used) return;
            Clicked?.Invoke();
        }
    }
}
