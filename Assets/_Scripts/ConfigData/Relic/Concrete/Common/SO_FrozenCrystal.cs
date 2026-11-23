using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>永冻冰晶：本场首次打出能力牌时获得格挡。</summary>
    [CreateAssetMenu(fileName = "FrozenCrystal", menuName = "DreamKeep/Data/Relic/永冻冰晶")]
    public sealed class SO_FrozenCrystal : SO_Relic
    {
        [SerializeField] private int _block = 4;

        public int Block => _block;
    }
}
