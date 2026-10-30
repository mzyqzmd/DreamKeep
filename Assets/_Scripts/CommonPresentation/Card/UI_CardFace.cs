using System.Collections.Generic;
using System.Text;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>牌面：卡牌的纯展示层——把卡模型刷到名称 / 类型 / 费用 / 描述 / 图标上，并按稀有度换边框 / 名称横幅 / 类型铭牌三张图，
    /// 无费用卡隐藏费用块，不可打时整卡压暗；换图开关关掉的牌面（如诅咒）保持编辑器里摆好的固定外观。
    /// 它不参与任何交互：不处理指针事件、不管位置与缩放，只被战斗卡（UI_CardBattle）持有并转发调用；
    /// 战斗卡与展示卡共用这一张预制体，区别只在"有没有被包一层战斗卡"——图鉴之类的静态界面直接用它即纯展示卡。
    /// 数值预览由两个渲染器承接：卡面文本用压暗款、悬停气泡用原色款（求值与着色规则住在 CardValueRenderer，本类只配置颜色与转发）。</summary>
    public sealed class UI_CardFace : MonoBehaviour
    {
        [Header("文本")] [Tooltip("卡面费用文本")] [SerializeField]
        private TextMeshProUGUI _cost;

        [Tooltip("卡面名称文本")] [SerializeField] private TextMeshProUGUI _name;

        [Tooltip("卡面类型文本")] [SerializeField] private TextMeshProUGUI _type;

        [Tooltip("卡面描述文本")] [SerializeField] private TextMeshProUGUI _description;

        [Header("图形")] [Tooltip("卡面图标")] [SerializeField]
        private Image _icon;

        [Tooltip("卡面外框")] [SerializeField] private Image _border;
        [Tooltip("卡面名称横幅")] [SerializeField] private Image _nameBanner;
        [Tooltip("卡面类型横幅")] [SerializeField] private Image _typePlaque;

        [Tooltip("卡牌费用部分：费用为负的卡整块隐藏")] [SerializeField]
        private GameObject _cardCost;

        [Tooltip("涟漪动效边框节点：牌堆查看等纯展示场合经 SetRippleVisible 关掉")] [SerializeField]
        private GameObject _ripple;

        [Header("颜色")] [Tooltip("升级后卡牌名字字体颜色")] [SerializeField]
        private Color _upgradeNameColor = Color.green;

        [Tooltip("费用低于基础值时的字体颜色（减费提示，高于或等于保持本色）")] [SerializeField]
        private Color _costLowerColor = Color.green;

        [Tooltip("不可打时整卡乘的暗灰色")] [SerializeField]
        private Color _unplayableTint = new(0.45f, 0.45f, 0.5f);

        [Tooltip("关键词（不展示描述的词条）那一行名字的颜色：富文本标签色盖得住组件色，故不可打时按 _unplayableTint 另行压暗")] [SerializeField]
        private Color _affixNameColor = new(0.9622642f, 0.893509f, 0.0127091f);

        [Tooltip("数值被修正得更高时的颜色（绿）")] [SerializeField]
        private Color _valueBoostColor = Color.green;

        [Tooltip("数值被修正得更低时的颜色（红）")] [SerializeField]
        private Color _valueReduceColor = new(1f, 0.36f, 0.36f);

        // 预览状态：可打标记与两个数值渲染器（卡面文本款按不可打状态压暗、气泡款恒原色；门面与目标由手牌区每帧推入）
        private bool _playable = true;
        private CardValueRenderer _faceValueRenderer;
        private CardValueRenderer _tipValueRenderer;
        private readonly StringBuilder _descriptionBuilder = new(); // 描述文本的复用缓冲：每次重排只清空不重建

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

        // 生命周期：建两个数值渲染器组（颜色取编辑器值；卡面款的压暗随可打状态在 SetPlayableLook 里更新）
        private void Awake()
        {
            _faceValueRenderer = new CardValueRenderer(_valueBoostColor, _valueReduceColor, _costLowerColor);
            _tipValueRenderer = new CardValueRenderer(_valueBoostColor, _valueReduceColor, _costLowerColor);
        }

        #region 绑定卡面：刷新内容与外观，并把运行时卡模型收在牌面上

        public void Bind(CardModel card_)
        {
            // 色缓存与涟漪节点懒建：卡池复用的实例每次 Bind 都要重写内容，守卫只包建缓存这一段
            EnsureCache();

            //设置卡面
            Card = card_;
            // 预览门面随卡重置：防池化实例把上一张卡的修正值串到新卡（手牌区下一帧的预览对账会重填）
            SetPreviewContext(null, null);
            _name.text = card_.Name;
            _type.text = card_.GetTypeDescription();
            _icon.sprite = card_.Icon;
            // 描述文本不在这里写：关键词行的黄色要随可打状态一起压暗，故与整卡外观同在 SetPlayableLook 里刷新
            // 费用为负即无费用，整块费用不展示；文本与颜色由 SetPlayableLook 经渲染器统一写（随不可打压暗）
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

        // 缓存懒建：收齐卡面全部图形/文本与它们的编辑期本色、按名找出涟漪动效节点——集合与本色运行期不变，
        // 首次 Bind 建一次即可反复用；card_ripple 优先用编辑器接线，未接则按名在直接子节点找（四张脸都摆着，漏摆留 null、开关静默跳过）
        private void EnsureCache()
        {
            if (_faceImages != null) return;
            _faceImages = GetComponentsInChildren<Image>(true);
            _faceTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            _imageColors = new Color[_faceImages.Length];
            _textColors = new Color[_faceTexts.Length];
            for (int i = 0; i < _faceImages.Length; i++) _imageColors[i] = _faceImages[i].color;
            for (int i = 0; i < _faceTexts.Length; i++) _textColors[i] = _faceTexts[i].color;
        }

        #endregion


        #region 不可打外观与描述重建：整卡图形与文本乘同一个灰调，可打时写回本色

        public void SetPlayableLook(bool playable_)
        {
            _playable = playable_;
            // 卡面数值的颜色跟着压暗走（富文本标签色盖得住组件色，必须显式预乘）；气泡款恒原色不动
            Color tint = playable_ ? Color.white : _unplayableTint;
            _faceValueRenderer.SetColors(_valueBoostColor * tint, _valueReduceColor * tint, _costLowerColor * tint);
            _cost.text = _faceValueRenderer.Render(Card.Cost.BaseValue, Card.Cost.Value, EStatChannel.Cost);

            RebuildDescription();
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

        #region 涟漪边框开关：牌堆查看等纯展示场合关掉 card_ripple 动效（战斗路径不调，默认照旧）

        /// <summary>开关 card_ripple 动效边框：动效节点失活即停、重开自动重播；节点缺失静默跳过，调用方无需判空。
        /// 必须在 Bind 之后调用（涟漪节点随 EnsureCache 懒建）。</summary>
        public void SetRippleVisible(bool visible_)
        {
            if (_ripple == null || _ripple.activeSelf == visible_) return;
            _ripple.SetActive(visible_);
        }

        #endregion

        #region 数值预览：门面转发与描述重建（求值与着色的唯一落点是 CardValueRenderer）

        /// <summary>写入数值预览门面与目标集合（手牌区对账时推入；门面为空时伤害与格挡不做 perk 修正、
        /// 直接与升级前基准静态比较着色，目标集合为空或为 null 时目标侧修正不参与）。
        /// 卡面与气泡两个渲染器推同一份，两者天然一致。</summary>
        public void SetPreviewContext(BattleQueryDispatcher queryDispatcher_, IReadOnlyList<Creature> targets_)
        {
            _faceValueRenderer.SetContext(queryDispatcher_, targets_);
            _tipValueRenderer.SetContext(queryDispatcher_, targets_);
        }

        /// <summary>收集悬停气泡的词条列（复用列表，先清后填）：只认带出词条的行（其余行由卡面自己陈列），
        /// 数值经气泡款渲染器求值与着色；战斗卡与牌堆卡共用，整列为空时气泡不弹。</summary>
        public void CollectTipLines(List<(string Name, string Description)> results_)
        {
            results_.Clear();
            IReadOnlyList<(string Text, CardAffix Affix)> lines = Card.GetDescriptionLines(_tipValueRenderer);
            for (int i = 0; i < lines.Count; i++)
                if (lines[i].Affix != null)
                    results_.Add((lines[i].Text, lines[i].Affix.GetDescription(_tipValueRenderer)));
        }

        /// <summary>重建描述文本：不展示描述的词条那一行只写词条名（黄 + 下划线，说明交给悬停/拖拽时弹出的气泡），
        /// 其余行的数值经渲染器着色（高绿低红、相等无色），不可打时颜色随整卡压暗。</summary>
        public void RebuildDescription()
        {
            string keywordColor = ColorUtility.ToHtmlStringRGB(
                _playable ? _affixNameColor : _affixNameColor * _unplayableTint);
            _descriptionBuilder.Clear();
            IReadOnlyList<(string Text, CardAffix Affix)> lines = Card.GetDescriptionLines(_faceValueRenderer);
            for (int i = 0; i < lines.Count; i++)
            {
                (string text, CardAffix affix) = lines[i];
                _descriptionBuilder.AppendLine(affix == null ? text : $"<color=#{keywordColor}><u>{text}</u></color>");
            }

            _description.text = _descriptionBuilder.ToString();
        }

        #endregion
    }
}