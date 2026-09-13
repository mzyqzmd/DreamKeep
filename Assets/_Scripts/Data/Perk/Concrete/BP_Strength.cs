using System;

namespace _DreamKeep.Data
{
    /// <summary>状态「力量」：每层提高 1 点攻击伤害，战斗内不自动衰减。</summary>
    [Serializable]
    public class BP_Strength : BattlePerk
    {
        public override string Name => "力量";
        public override bool DecaysEachTurn => false;
        public override string GetDescription() => $"从卡牌中造成的伤害提高 {Layers} 点";
        public override BattlePerk Clone() => WithLayers(new BP_Strength());
    }
}
