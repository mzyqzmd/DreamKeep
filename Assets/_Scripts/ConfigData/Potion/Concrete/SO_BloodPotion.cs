using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>鲜血药水：按最大生命的比例回复（结算时按当时的生命上限现算）。</summary>
    [CreateAssetMenu(fileName = "BloodPotion", menuName = "DreamKeep/Data/Potion/鲜血药水")]
    public sealed class SO_BloodPotion : SO_Potion
    {
        [Range(0f, 1f)]
        [SerializeField] private float _healPercent = 0.2f;

        public float HealPercent => _healPercent;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;

        /// <summary>纯回血、不依赖战斗态，战斗外也能喝。</summary>
        public override bool UsableOutsideBattle => true;
    }
}
