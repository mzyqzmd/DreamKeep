using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>孙子兵法：本回合未打出攻击牌时，下个玩家回合获得能量。</summary>
    [CreateAssetMenu(fileName = "ArtOfWar", menuName = "DreamKeep/Data/Relic/孙子兵法")]
    public sealed class SO_ArtOfWar : SO_Relic
    {
        [SerializeField] private int _energy = 1;

        public int Energy => _energy;
    }
}
