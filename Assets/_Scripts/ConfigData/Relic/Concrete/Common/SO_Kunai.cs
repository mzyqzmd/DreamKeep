using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>苦无：每回合每打出若干张攻击牌，获得敏捷。</summary>
    [CreateAssetMenu(fileName = "Kunai", menuName = "DreamKeep/Data/Relic/苦无")]
    public sealed class SO_Kunai : SO_Relic
    {
        [SerializeField] private int _count = 3;
        [SerializeField] private SO_Dexterity _dexterity;
        [SerializeField] private int _layers = 1;

        public int Count => _count;
        public SO_Dexterity Dexterity => _dexterity;
        public int Layers => _layers;
    }
}
