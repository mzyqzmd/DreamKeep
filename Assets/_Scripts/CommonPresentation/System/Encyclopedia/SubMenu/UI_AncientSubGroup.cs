using _DreamKeep.ConfigData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>遗物百科的先古子组：一位先古之民的头像、名号与遗物格子容器。
    /// 三个子对象由模板节点上的序列化引用直接给出，取代按字符串层级路径查找。</summary>
    public sealed class UI_AncientSubGroup : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private RectTransform _group;

        /// <summary>遗物格子容器：本组槽位按此父级陈列。</summary>
        public RectTransform Group => _group;

        public void Bind(SO_Ancient ancient_)
        {
            _icon.sprite = ancient_.AncientView.Portrait;
            _name.text = ancient_.AncientName;
        }
    }
}
