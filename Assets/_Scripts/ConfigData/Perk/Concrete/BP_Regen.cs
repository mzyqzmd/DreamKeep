using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「再生」：回合结束恢复等于层数的生命，随后层数递减。</summary>
    [Serializable]
    public class BP_Regen : BattlePerk
    {
        public override string Name => "再生";
        public override string GetDescription() => $"回合结束恢复 {Layers} 点生命值，随后层数减少一层";
        public override BattlePerk Clone() => WithLayers(new BP_Regen());
    }
}
