using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>熔炉的祝福：本场战斗中升级手牌中的所有牌，战斗结束即失效。</summary>
    [CreateAssetMenu(fileName = "BlessingOfTheForge", menuName = "DreamKeep/Data/Potion/熔炉的祝福")]
    public sealed class SO_BlessingOfTheForge : SO_Potion
    {
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
