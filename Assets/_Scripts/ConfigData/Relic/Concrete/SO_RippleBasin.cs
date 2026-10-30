using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>波纹水盆：本回合未打出攻击牌时，回合结束获得格挡。</summary>
    [CreateAssetMenu(fileName = "RippleBasin", menuName = "DreamKeep/Data/Relic/波纹水盆")]
    public sealed class SO_RippleBasin : SO_Relic
    {
        [SerializeField] private int _block = 4;

        public int Block => _block;
    }
}
