using _DreamKeep.ConfigData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>状态槽位：PerkGroup 里的一格，显示状态的图标与当前层数（恒显示含 1 层）；
    /// 图标、层数与状态数据由 Bind 写入，悬停判定与说明气泡统一由宿主 UI_PerkGroup 承担。</summary>
    public class UI_PerkSlot : MonoBehaviour
    {
        [SerializeField] private Image _perkIcon;
        [SerializeField] private TextMeshProUGUI _perkAmount;

        /// <summary>绑定的挂载实例：图标、层数与悬停气泡内容都取自它。</summary>
        public BattlePerk Perk { get; private set; }

        public void Bind(BattlePerk perk_)
        {
            Perk = perk_;
            _perkIcon.sprite = perk_.Icon;
            _perkAmount.text = perk_.Layers.ToString();
        }

        // 层数刷新：域侧递减后由宿主调用，恒显示当前层数（含 1 层）
        public void RefreshLayers() => _perkAmount.text = Perk.Layers.ToString();
    }
}
