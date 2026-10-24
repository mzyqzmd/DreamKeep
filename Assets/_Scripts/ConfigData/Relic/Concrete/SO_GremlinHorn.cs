using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>地精之角：每当有一名敌人死亡时，获得能量并抽牌。</summary>
    [CreateAssetMenu(fileName = "GremlinHorn", menuName = "DreamKeep/Data/Relic/地精之角")]
    public sealed class SO_GremlinHorn : SO_Relic
    {
        [SerializeField] private int _energy = 1;
        [SerializeField] private int _cards = 1;

        public int Energy => _energy;
        public int Cards => _cards;
    }
}
