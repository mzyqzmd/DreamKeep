using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>金色珍珠：拾起时一次性获得金币，无持续效果（结算由先古段负责）。</summary>
    [CreateAssetMenu(fileName = "GoldenPearl", menuName = "DreamKeep/Data/Relic/金色珍珠")]
    public sealed class SO_GoldenPearl : SO_Relic
    {
        [SerializeField] private int _gold = 150;

        public int Gold => _gold;
    }
}
