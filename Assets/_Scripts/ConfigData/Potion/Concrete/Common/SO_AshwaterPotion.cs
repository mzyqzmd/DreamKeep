using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>灰水：使用后从手牌中任选若干张（可为 0 张）消耗，无其余效果。</summary>
    [CreateAssetMenu(fileName = "Ashwater", menuName = "DreamKeep/Data/Potion/灰水")]
    public sealed class SO_AshwaterPotion : SO_Potion
    {
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
