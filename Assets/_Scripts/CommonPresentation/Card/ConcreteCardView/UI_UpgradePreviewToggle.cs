using System;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>升级预览开关行：特写里"升级预览"那一行（可点框 + 勾选图标），决定特写卡该展示哪张——
    /// 开关语义统一为"是否看升级后形态"：未升级卡关=当前形态、开=临时副本套升级补丁（不写回域）；
    /// 已升级卡开=当前形态、关=基础版（烘焙原型）。行显隐=可升级或已升级，行显示即可点
    /// （行隐藏时按钮整个 inactive 天然点不了）。卡组面板特写与三选一详情特写共用本组件，
    /// 翻转经 Changed 通知面板重绑。</summary>
    [RequireComponent(typeof(Button))]
    public sealed class UI_UpgradePreviewToggle : MonoBehaviour
    {
        [Tooltip("勾选图标：随预览态换图")] [SerializeField] private Image _icon;

        [Tooltip("预览开的勾选图")] [SerializeField] private Sprite _tickSprite;

        [Tooltip("预览关的空心框图")] [SerializeField] private Sprite _untickSprite;

        private Button _button;
        private CardModel _card; // 特写正在看的卡（域内真卡的引用）
        private bool _showingUpgraded; // 当前是否处于"看升级后形态"

        /// <summary>开关翻转后通知面板重绑特写。</summary>
        public event Action Changed;

        /// <summary>当前该展示的卡：纯展示口径，任何分支都不写回域。</summary>
        public CardModel DisplayCard
        {
            get
            {
                if (_card == null) return null;
                if (_showingUpgraded) return _card.IsUpgraded ? _card : UpgradedCopyOf(_card);
                return _card.IsUpgraded ? _card.BaseVersion : _card;
            }
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Toggle);
        }

        private void OnDestroy() => _button.onClick.RemoveListener(Toggle);

        // 换特写的卡：按卡牌能力摆行（显隐、可点、默认勾选态=已升级），展示口径见 DisplayCard
        public void SetCard(CardModel card_)
        {
            _card = card_;
            _showingUpgraded = card_ != null && card_.IsUpgraded;
            gameObject.SetActive(card_ != null && (card_.CanUpgrade || card_.IsUpgraded));
            _button.interactable = card_ != null;
            WriteIcon();
        }

        private void Toggle()
        {
            _showingUpgraded = !_showingUpgraded;
            WriteIcon();
            Changed?.Invoke();
        }

        // 升级预览副本：克隆可变副本套升级补丁，一次性展示对象，不触域内真卡
        private static CardModel UpgradedCopyOf(CardModel card_)
        {
            CardModel copy = card_.ToMutable();
            copy.Upgrade();
            return copy;
        }

        private void WriteIcon()
        {
            if (_icon) _icon.sprite = _showingUpgraded ? _tickSprite : _untickSprite;
        }
    }
}
