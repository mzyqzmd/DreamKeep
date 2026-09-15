using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace _DreamKeep.Presentation
{
    /// <summary>卡面视图：按运行时卡刷新卡面内容并接受落位，只做显示，不参与任何战斗规则。</summary>
    public class Comp_CardView : MonoBehaviour
    {
        [SerializeField] private SortingGroup _sortingGroup;
        [SerializeField] private TextMeshProUGUI _cost;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _type;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private SpriteRenderer _icon;

        private CardModel _card;
        private SpriteRenderer[] _spriteRenderers;
        private TMP_Text[] _texts;

        public CardModel Card => _card;

        public int SortingOrder => _sortingGroup.sortingOrder;

        public float Alpha { get; private set; } = 1f;

        // 卡面由若干精灵与若干独立 world-space Canvas 的文本拼成，透明度要统一改，先一次性收集
        private void Awake()
        {
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            _texts = GetComponentsInChildren<TMP_Text>(true);
        }

        #region 绑定一张运行时卡：名称/费用/描述/图标/类型全部取自模型统一入口，不触达配置

        public void Bind(CardModel card_)
        {
            _card = card_;
            _name.text = card_.Name;
            _type.text = card_.GetTypeDescription();
            _icon.sprite = card_.Icon;
            _cost.text = card_.Cost.Value.ToString();
            _description.text = card_.GetDescription();
        }

        #endregion

        #region 落位：设置世界坐标、朝向与层序

        public void Place(Vector3 position_, Quaternion rotation_, int order_)
        {
            transform.position = position_;
            transform.rotation = rotation_;
            _sortingGroup.sortingOrder = order_;
        }

        #endregion

        #region 整体透明度：改写卡面所有精灵与文本的 alpha，供入场淡入与离场淡出使用

        public void SetAlpha(float alpha_)
        {
            Alpha = alpha_;
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                Color color = _spriteRenderers[i].color;
                color.a = alpha_;
                _spriteRenderers[i].color = color;
            }

            for (int i = 0; i < _texts.Length; i++)
            {
                Color color = _texts[i].color;
                color.a = alpha_;
                _texts[i].color = color;
            }
        }

        #endregion
    }
}