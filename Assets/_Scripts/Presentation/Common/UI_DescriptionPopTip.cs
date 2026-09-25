using _DreamKeep.ConfigData;
using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>状态详情提示：名字与描述的复用弹板，内容由 UI_InfoPanel 在弹出前经 SetContent 写入，显隐与定位也归它管。</summary>
    public class UI_DescriptionPopTip : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _description;

        public void SetContent(string name_, string description_)
        {
            _name.text = name_;
            _description.text = description_;
        }
    }
}