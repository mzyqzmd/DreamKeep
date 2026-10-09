using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>牌面：卡牌的纯展示层——把卡模型刷到名称 / 类型 / 费用 / 描述 / 图标与外框配色上，并提供不可打外观的整卡压暗。
    /// 它不参与任何交互：不挂命中面、不收指针事件、不管位置与缩放，只被包装（UI_CardHolder）持有并转发调用；
    /// 战斗卡与展示卡共用这一张预制体，区别只在"有没有被包一层包装"——图鉴之类的静态界面直接用它即纯展示卡。</summary>
    public sealed class UI_CardFace : MonoBehaviour
    {
        [Tooltip("卡面费用文本，内容来自 CardModel.Cost")] [SerializeField]
        private TextMeshProUGUI _cost;

        [Tooltip("卡面名称文本，内容来自 CardModel.Name")] [SerializeField]
        private TextMeshProUGUI _name;

        [Tooltip("卡面类型文本，内容来自 CardModel.GetTypeDescription()")] [SerializeField]
        private TextMeshProUGUI _type;

        [Tooltip("卡面描述文本，内容来自 CardModel.GetDescription()")] [SerializeField]
        private TextMeshProUGUI _description;

        [Tooltip("卡面图标，内容来自 CardModel.Icon")] [SerializeField]
        private Image _icon;

        [Tooltip("卡牌外框：稀有度染色的载体（指针命中面归包装，这里所有图形都不参与命中）")] [SerializeField]
        private Image _frame;

        [Tooltip("普通卡外框色")] [SerializeField]
        private Color _frameColorNormal = new(0.82f, 0.84f, 0.88f);

        [Tooltip("稀有卡外框色")] [SerializeField]
        private Color _frameColorRare = new(0.45f, 0.72f, 1f);

        [Tooltip("超稀有卡外框色")] [SerializeField]
        private Color _frameColorSuperRare = new(1f, 0.84f, 0.42f);

        [Tooltip("不可打时整卡乘的暗灰色：与卡面本色相乘写回，稀有度外框差异得以保留")] [SerializeField]
        private Color _unplayableTint = new(0.45f, 0.45f, 0.5f);

        private Image[] _faceImages;
        private TextMeshProUGUI[] _faceTexts;
        private Color[] _imageColors;
        private Color[] _textColors;
        private Color _frameBase;
        private bool _playable = true;

        public CardModel Card { get; private set; }

        // 收齐卡面全部图形与它们的编辑期本色：不可打外观的乘色以本色为基准，可打时逐字写回
        private void Awake()
        {
            _faceImages = GetComponentsInChildren<Image>(true);
            _faceTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            _imageColors = new Color[_faceImages.Length];
            _textColors = new Color[_faceTexts.Length];
            for (int i = 0; i < _faceImages.Length; i++) _imageColors[i] = _faceImages[i].color;
            for (int i = 0; i < _faceTexts.Length; i++) _textColors[i] = _faceTexts[i].color;
        }

        #region 绑定卡面：刷新显示并把运行时卡模型收在牌面上

        public void Bind(CardModel card_)
        {
            Card = card_;
            _name.text = card_.Name;
            _type.text = card_.GetTypeDescription();
            _icon.sprite = card_.Icon;
            _cost.text = card_.Cost.Value.ToString();
            _description.text = card_.GetDescription();
            _frameBase = card_.Rarity switch
            {
                ECardRarity.Rare => _frameColorRare,
                ECardRarity.SuperRare => _frameColorSuperRare,
                _ => _frameColorNormal
            };
            // 外框色随稀有度变，统一由 SetPlayableLook 写；池复用后残留的压暗色与旧卡本色借此一并刷正
            SetPlayableLook(_playable);
        }

        #endregion

        #region 不可打外观：整卡图形与文本乘同一个灰调，可打时写回本色

        public void SetPlayableLook(bool playable_)
        {
            _playable = playable_;
            for (int i = 0; i < _faceImages.Length; i++)
                _faceImages[i].color = playable_ ? _imageColors[i] : _imageColors[i] * _unplayableTint;
            for (int i = 0; i < _faceTexts.Length; i++)
                _faceTexts[i].color = playable_ ? _textColors[i] : _textColors[i] * _unplayableTint;
            // 外框色由 Bind 存进 _frameBase，最后单独覆盖，避免被上面那批编辑期本色写回底色
            _frame.color = playable_ ? _frameBase : _frameBase * _unplayableTint;
        }

        #endregion
    }
}
