using System.Collections.Generic;
using System.Text;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>牌面：卡牌的纯展示层——把卡模型刷到名称 / 类型 / 费用 / 描述 / 图标上，并按稀有度换边框 / 名称横幅 / 类型铭牌三张图，
    /// 无费用卡隐藏费用块，不可打时整卡压暗；换图开关关掉的牌面（如诅咒）保持编辑器里摆好的固定外观。
    /// 它不参与任何交互：不处理指针事件、不管位置与缩放，只被战斗卡（UI_CardBattle）持有并转发调用；
    /// 战斗卡与展示卡共用这一张预制体，区别只在"有没有被包一层战斗卡"——图鉴之类的静态界面直接用它即纯展示卡。</summary>
    public sealed class UI_CardFace : MonoBehaviour
    {
        [Header("文本")] [Tooltip("卡面费用文本")] [SerializeField]
        private TextMeshProUGUI _cost;

        [Tooltip("卡面名称文本")] [SerializeField] private TextMeshProUGUI _name;

        [Tooltip("卡面类型文本")] [SerializeField] private TextMeshProUGUI _type;

        [Tooltip("卡面描述文本")] [SerializeField] private TextMeshProUGUI _description;

        [Header("图形")] [Tooltip("卡面图标")] [SerializeField]
        private Image _icon;

        [Tooltip("卡面背景")] [SerializeField] private Image _background;
        [Tooltip("卡面外框")] [SerializeField] private Image _border;
        [Tooltip("卡面名称横幅")] [SerializeField] private Image _nameBanner;
        [Tooltip("卡面类型横幅")] [SerializeField] private Image _typePlaque;

        [Tooltip("卡牌费用部分：费用为负的卡整块隐藏")] [SerializeField]
        private GameObject _cardCost;

        [Tooltip("升级后卡牌名字字体颜色")] [SerializeField]
        private Color _upgradeNameColor = Color.green;

        [Tooltip("不可打时整卡乘的暗灰色")] [SerializeField]
        private Color _unplayableTint = new(0.45f, 0.45f, 0.5f);

        [Tooltip("关键词（不展示描述的词条）那一行名字的颜色：富文本标签色盖得住组件色，故不可打时按 _unplayableTint 另行压暗")] [SerializeField]
        private Color _affixNameColor = new(0.9622642f, 0.893509f, 0.0127091f);

        #region 卡牌资源引用

        [Header("卡牌资源")] [Tooltip("是否需要按稀有度换图：关掉的牌面（如诅咒）三张图保持编辑器里摆好的固定外观")] [SerializeField]
        private bool _needCardSources;

        [Header("普通卡牌资源")] [ShowIf("_needCardSources"), SerializeField]
        private Sprite _normalBorder;

        [ShowIf("_needCardSources"), SerializeField]
        private Sprite _normalNameBanner;

        [ShowIf("_needCardSources"), SerializeField]
        private Sprite _normalTypePlaque;

        [Header("稀有卡牌资源")] [ShowIf("_needCardSources"), SerializeField]
        private Sprite _rareBorder;

        [ShowIf("_needCardSources"), SerializeField]
        private Sprite _rareNameBanner;

        [ShowIf("_needCardSources"), SerializeField]
        private Sprite _rareTypePlaque;

        [Header("超稀有卡牌资源")] [ShowIf("_needCardSources"), SerializeField]
        private Sprite _superRareBorder;

        [ShowIf("_needCardSources"), SerializeField]
        private Sprite _superRareNameBanner;

        [ShowIf("_needCardSources"), SerializeField]
        private Sprite _superRareTypePlaque;

        #endregion

        private Image[] _faceImages;
        private TextMeshProUGUI[] _faceTexts;
        private Color[] _imageColors;
        private Color[] _textColors;

        public CardModel Card { get; private set; }

        #region 绑定卡面：刷新内容与外观，并把运行时卡模型收在牌面上

        public void Bind(CardModel card_)
        {
            // 色缓存懒建：收齐卡面全部图形与它们的编辑期本色——图形集合与本色运行期不变，首次 Bind 建一次即可反复用。
            // 守卫只包这一段：卡池复用的实例每次 Bind 都要重写内容，否则内容会停在上一次绑定的那张卡
            if (_faceImages == null)
            {
                _faceImages = GetComponentsInChildren<Image>(true);
                _faceTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
                _imageColors = new Color[_faceImages.Length];
                _textColors = new Color[_faceTexts.Length];
                for (int i = 0; i < _faceImages.Length; i++) _imageColors[i] = _faceImages[i].color;
                for (int i = 0; i < _faceTexts.Length; i++) _textColors[i] = _faceTexts[i].color;
            }

            //设置卡面
            Card = card_;
            _name.text = card_.Name;
            _type.text = card_.GetTypeDescription();
            _icon.sprite = card_.Icon;
            // 描述文本不在这里写：关键词行的黄色要随可打状态一起压暗，故与整卡外观同在 SetPlayableLook 里刷新
            // 费用为负即无费用，整块费用不展示；其余卡照常写数字
            _cost.text = card_.Cost.Value.ToString();
            _cardCost.SetActive(card_.Cost.Value >= 0);
            // 按稀有度换边框三图；不需要换图的牌面（如诅咒）保持编辑器里摆好的固定外观
            if (_needCardSources)
            {
                switch (card_.Rarity)
                {
                    case ECardRarity.Rare:
                        _border.sprite = _rareBorder;
                        _nameBanner.sprite = _rareNameBanner;
                        _typePlaque.sprite = _rareTypePlaque;
                        break;
                    case ECardRarity.SuperRare:
                        _border.sprite = _superRareBorder;
                        _nameBanner.sprite = _superRareNameBanner;
                        _typePlaque.sprite = _superRareTypePlaque;
                        break;
                    default:
                        _border.sprite = _normalBorder;
                        _nameBanner.sprite = _normalNameBanner;
                        _typePlaque.sprite = _normalTypePlaque;
                        break;
                }
            }

            // 初始化可打状态
            SetPlayableLook(true);
        }

        #endregion


        #region 不可打外观：整卡图形与文本乘同一个灰调，可打时写回本色

        public void SetPlayableLook(bool playable_)
        {
            // 描述文本的书写：不展示描述的词条那一行只写词条名（黄 + 下划线，说明交给悬停/拖拽时弹出的气泡），其余行原样。
            string keywordColor = ColorUtility.ToHtmlStringRGB(
                playable_ ? _affixNameColor : _affixNameColor * _unplayableTint);
            StringBuilder description = new();
            IReadOnlyList<(string Text, CardAffix Affix)> lines = Card.GetDescriptionLines();
            for (int i = 0; i < lines.Count; i++)
            {
                (string text, CardAffix affix) = lines[i];
                description.AppendLine(affix == null ? text : $"<color=#{keywordColor}><u>{text}</u></color>");
            }

            _description.text = description.ToString();
            for (int i = 0; i < _faceImages.Length; i++)
                _faceImages[i].color = playable_ ? _imageColors[i] : _imageColors[i] * _unplayableTint;
            for (int i = 0; i < _faceTexts.Length; i++)
            {
                // 名字基色随升级状态：升级为绿色、否则白；不可打再统一乘暗灰
                Color baseColor = _faceTexts[i] == _name
                    ? (Card.IsUpgraded ? _upgradeNameColor : Color.white)
                    : _textColors[i];
                _faceTexts[i].color = playable_ ? baseColor : baseColor * _unplayableTint;
            }
        }

        #endregion

        [Button("卡牌升级测试按钮", EButtonEnableMode.Playmode)]
        public void Upgrade()
        {
            Card.Upgrade();
            Bind(Card);
        }
    }
}