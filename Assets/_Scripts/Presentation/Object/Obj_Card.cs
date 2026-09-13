using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace _DreamKeep.Presentation
{
    /// <summary>卡面视图：按运行时卡刷新卡面内容并接受落位，只做显示，不参与任何战斗规则。</summary>
    public class Obj_Card : MonoBehaviour
    {
        [SerializeField] private SortingGroup _sortingGroup;
        [SerializeField] private TextMeshProUGUI _cost;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _type;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private SpriteRenderer _icon;

        private CardModel _card;

        public CardModel Card => _card;

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
    }
}