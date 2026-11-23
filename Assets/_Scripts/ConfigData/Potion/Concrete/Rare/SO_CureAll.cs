using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>痊愈药水：获得能量并抽牌。</summary>
    [CreateAssetMenu(fileName = "CureAll", menuName = "DreamKeep/Data/Potion/痊愈药水")]
    public sealed class SO_CureAll : SO_Potion
    {
        [SerializeField] private int _energy = 1;
        [SerializeField] private int _cards = 2;

        public int Energy => _energy;
        public int Cards => _cards;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
