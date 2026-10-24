using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>开心小花：每 N 个回合获得能量（回合序取模，能量重置之后结算）。</summary>
    [CreateAssetMenu(fileName = "HappyFlower", menuName = "DreamKeep/Data/Relic/开心小花")]
    public sealed class SO_HappyFlower : SO_Relic
    {
        [SerializeField] private int _turnInterval = 3;
        [SerializeField] private int _energy = 1;

        public int TurnInterval => _turnInterval;
        public int Energy => _energy;
    }
}
