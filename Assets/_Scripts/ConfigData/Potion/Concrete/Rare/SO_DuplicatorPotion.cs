using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>复制药水：给自己施加复制状态，下一张牌额外打出一次。</summary>
    [CreateAssetMenu(fileName = "DuplicatorPotion", menuName = "DreamKeep/Data/Potion/复制药水")]
    public sealed class SO_DuplicatorPotion : SO_Potion
    {
        [SerializeField] private SO_Duplication _duplication;

        public SO_Duplication Duplication => _duplication;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
