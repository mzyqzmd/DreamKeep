using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _DreamKeep.ConfigData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>遗物信息面板：由 PopPanelFactory 懒建的单实例弹层，顶栏遗物槽位点击与商店遗物格右键共用，
    /// 写入该遗物的名称、稀有度、图标与描述，稀有度一行按档位着色（初始/普通白、稀有金黄、商店天蓝、远古暗红），
    /// 已用尽的遗物在描述末尾补一行提示；点击面板外的遮罩收起，点面板框内不响应。</summary>
    public sealed class UI_RelicsInfoPanel : MonoBehaviour, IPointerClickHandler
    {
        // 用尽后补在说明末尾的一行：悬停气泡与信息面板共用这一句，故对外公开
        public const string ExhaustedTip = "这个遗物的效果已经耗尽";

        [Tooltip("面板框：点它的矩形内不收起")] [SerializeField]
        private RectTransform _infoContainer;

        [Tooltip("遗物名称")] [SerializeField] private TextMeshProUGUI _relicName;

        [Tooltip("稀有度一行：文案与颜色都随稀有度改写")] [SerializeField]
        private TextMeshProUGUI _relicRarity;

        [Tooltip("遗物图标")] [SerializeField] private Image _relicIcon;

        [Tooltip("遗物描述：配置里的 TMP 富文本原样写入")] [SerializeField]
        private TextMeshProUGUI _description;

        [Header("稀有度颜色")] [Tooltip("初始遗物与普通遗物的颜色")] [SerializeField]
        private Color _normalColor = Color.white;

        [Tooltip("稀有遗物的颜色")] [SerializeField] private Color _rareColor = new(1f, 0.83f, 0.3f);

        [Tooltip("商店遗物的颜色")] [SerializeField] private Color _shopColor = new(0.45f, 0.72f, 1f);

        [Tooltip("远古遗物的颜色（偏暗的亮红）")] [SerializeField]
        private Color _ancientColor = new(0.8f, 0.2f, 0.24f);

        #region 展示一件遗物

        /// <summary>写入遗物的四要素并按稀有度着色，已用尽则在描述末尾补提示，随后显示面板。
        /// 面板是单实例复用的：换一件遗物再调一次就地把内容改掉，不重建节点。</summary>
        public void Show(SO_Relic relic_, bool exhausted_)
        {
            _relicName.text = relic_.RelicName;
            _relicIcon.sprite = relic_.Icon;
            (string text, Color color) = RarityView(relic_.Rarity);
            _relicRarity.text = text;
            _relicRarity.color = color;
            _description.text = exhausted_
                ? relic_.Description + "\n" + ExhaustedTip
                : relic_.Description;
            gameObject.SetActive(true);
        }

        #endregion

        // 点遮罩收起：判据是点在面板框矩形外（子节点怎么改射线目标都不影响）
        public void OnPointerClick(PointerEventData eventData_)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(_infoContainer, eventData_.position,
                    eventData_.pressEventCamera)) return;
            gameObject.SetActive(false);
        }

        // 稀有度 → 文案与颜色：未登记的值按普通遗物处理
        private (string Text, Color Color) RarityView(ERelicRarity rarity_) => rarity_ switch
        {
            ERelicRarity.Starter => ("初始遗物", _normalColor),
            ERelicRarity.Rare => ("稀有遗物", _rareColor),
            ERelicRarity.Shop => ("商店遗物", _shopColor),
            ERelicRarity.Ancient => ("远古遗物", _ancientColor),
            _ => ("普通遗物", _normalColor),
        };
    }
}
