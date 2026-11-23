using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>奥利哈钢：回合结束时若没有格挡，获得格挡（回合结束时刻格挡尚未被清，清除只发生在下回合开始）。</summary>
    [CreateAssetMenu(fileName = "Orichalcum", menuName = "DreamKeep/Data/Relic/奥利哈钢")]
    public sealed class SO_Orichalcum : SO_Relic
    {
        [SerializeField] private int _block = 6;

        public int Block => _block;
    }
}
