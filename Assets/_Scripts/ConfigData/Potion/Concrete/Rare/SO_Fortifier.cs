using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>固化药水：把当前格挡变成三倍（实现为再获得当前格挡的两倍）。</summary>
    [CreateAssetMenu(fileName = "Fortifier", menuName = "DreamKeep/Data/Potion/固化药水")]
    public sealed class SO_Fortifier : SO_Potion
    {
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
