using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>双截棍：每回合每打出若干张攻击牌，获得能量。</summary>
    [CreateAssetMenu(fileName = "Nunchaku", menuName = "DreamKeep/Data/Relic/双截棍")]
    public sealed class SO_Nunchaku : SO_Relic
    {
        [SerializeField] private int _count = 3;
        [SerializeField] private int _energy = 1;

        public int Count => _count;
        public int Energy => _energy;
    }
}
