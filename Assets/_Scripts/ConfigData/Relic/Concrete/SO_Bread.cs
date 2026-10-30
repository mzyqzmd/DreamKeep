using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>面包：首回合少获得能量，其余回合每回合额外获得能量。</summary>
    [CreateAssetMenu(fileName = "Bread", menuName = "DreamKeep/Data/Relic/面包")]
    public sealed class SO_Bread : SO_Relic
    {
        [SerializeField] private int _energy = 1;
        [SerializeField] private int _firstTurnPenalty = 1;

        public int Energy => _energy;
        public int FirstTurnPenalty => _firstTurnPenalty;
    }
}
