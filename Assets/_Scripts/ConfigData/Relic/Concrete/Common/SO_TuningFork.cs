using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>音叉：每回合每打出若干张技能牌，获得格挡。</summary>
    [CreateAssetMenu(fileName = "TuningFork", menuName = "DreamKeep/Data/Relic/音叉")]
    public sealed class SO_TuningFork : SO_Relic
    {
        [SerializeField] private int _count = 3;
        [SerializeField] private int _block = 4;

        public int Count => _count;
        public int Block => _block;
    }
}
