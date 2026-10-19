using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>说明气泡：标题与描述的复用弹板，内容由发起器（UI_HoverTip）在弹出时写入；
    /// 标题为空自动收起标题行（意图类说明只有描述），实例取还归 PopRoot、显隐与定位归发起器。</summary>
    public sealed class UI_DescriptionPopTip : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _description;

        public void SetContent(string name_, string description_)
        {
            bool hasName = !string.IsNullOrEmpty(name_);
            _name.gameObject.SetActive(hasName);
            if (hasName) _name.text = name_;
            _description.text = description_;
        }
    }
}