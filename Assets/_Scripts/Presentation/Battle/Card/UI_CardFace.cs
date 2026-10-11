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

        [Tooltip("不可打时整卡乘的暗灰色")] [SerializeField]
        private Color _unplayableTint = new(0.45f, 0.45f, 0.5f);

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
            //缓存Image和Text
            // 收齐卡面全部图形与它们的编辑期本色：不可打外观的乘色以本色为基准，可打时逐字写回。
            _faceImages = GetComponentsInChildren<Image>(true);
            _faceTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            _imageColors = new Color[_faceImages.Length];
            _textColors = new Color[_faceTexts.Length];
            for (int i = 0; i < _faceImages.Length; i++) _imageColors[i] = _faceImages[i].color;
            for (int i = 0; i < _faceTexts.Length; i++) _textColors[i] = _faceTexts[i].color;
            //设置卡面
            Card = card_;
            _name.text = card_.Name;
            _type.text = card_.GetTypeDescription();
            _icon.sprite = card_.Icon;
            _description.text = card_.GetDescription();
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
            for (int i = 0; i < _faceImages.Length; i++)
                _faceImages[i].color = playable_ ? _imageColors[i] : _imageColors[i] * _unplayableTint;
            for (int i = 0; i < _faceTexts.Length; i++)
                _faceTexts[i].color = playable_ ? _textColors[i] : _textColors[i] * _unplayableTint;
        }

        #endregion
    }
}