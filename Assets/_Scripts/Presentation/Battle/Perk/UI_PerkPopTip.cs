using _DreamKeep.ConfigData;
using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>状态详情提示：名字与描述的复用弹板，内容由 UI_InfoPanel 在弹出前经 SetContent 写入，显隐与定位也归它管。</summary>
    public class UI_PerkPopTip : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _perkName;
        [SerializeField] private TextMeshProUGUI _perkDescription;

        public void SetContent(BattlePerk perk_)
        {
            _perkName.text = perk_.Name;
            _perkDescription.text = perk_.GetDescription();
        }
    }
}
