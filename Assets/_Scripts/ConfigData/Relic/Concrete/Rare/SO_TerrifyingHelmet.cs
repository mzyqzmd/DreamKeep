using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>骇人头盔：打出费用达到阈值的牌时获得格挡。</summary>
    [CreateAssetMenu(fileName = "TerrifyingHelmet", menuName = "DreamKeep/Data/Relic/骇人头盔")]
    public sealed class SO_TerrifyingHelmet : SO_Relic
    {
        [SerializeField] private int _costThreshold = 2;
        [SerializeField] private int _block = 3;

        public int CostThreshold => _costThreshold;
        public int Block => _block;
    }
}
