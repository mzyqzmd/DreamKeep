using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「地狱狂徒」定义：每当抽到名字含「打击」的牌时，对随机敌人自动打出该牌，常驻不衰减。</summary>
    [CreateAssetMenu(fileName = "Hellraiser", menuName = "DreamKeep/Data/Perk/地狱狂徒")]
    public sealed class SO_Hellraiser : SO_Perk
    {
        public override string Name => "地狱狂徒";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
