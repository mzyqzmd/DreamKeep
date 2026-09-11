using _DreamKeep.Data;
using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace _DreamKeep.Presentation
{
    /// <summary>卡面视图：按运行时卡与配置卡刷新卡面内容并接受落位，只做显示，不参与任何战斗规则。</summary>
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

        #region 绑定一张运行时卡：数值取运行时状态，名称与图标取配置
        public void Bind(CardModel card_, SO_Card config_)
        {
            _card = card_;
            _name.text = card_.Name;
            _type.text = GetTypeDescription(config_);
            _icon.sprite = config_.Icon;
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

        #region 卡类型描述：按配置卡类型翻译，配置缺失时兜底诅咒
        private string GetTypeDescription(SO_Card config_) => config_.Type switch
        {
            ECardType.Attack => "攻击",
            ECardType.Skill => "技能",
            ECardType.Ability => "能力",
            _ => "诅咒",
        };
        #endregion
    }
}
