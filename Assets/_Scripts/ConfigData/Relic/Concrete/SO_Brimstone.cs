using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>硫磺：每个玩家回合开始时自己获得力量、全体敌人也获得力量。</summary>
    [CreateAssetMenu(fileName = "Brimstone", menuName = "DreamKeep/Data/Relic/硫磺")]
    public sealed class SO_Brimstone : SO_Relic
    {
        [SerializeField] private SO_Strength _strength;
        [SerializeField] private int _selfLayers = 2;
        [SerializeField] private int _enemyLayers = 1;

        public SO_Strength Strength => _strength;
        public int SelfLayers => _selfLayers;
        public int EnemyLayers => _enemyLayers;
    }
}
