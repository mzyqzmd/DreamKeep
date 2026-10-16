using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「力量」运行时实例：每层提高 1 点攻击伤害，战斗内不自动衰减。</summary>
    public sealed class BP_Strength : BattlePerk
    {
        private PD_Strength Config => (PD_Strength)_data;

        public BP_Strength(PD_Strength data_) : base(data_) { }

        public override string GetDescription() => $"从卡牌中造成的伤害+ {Layers} 点";
        public override BattlePerk Clone() => WithLayers(new BP_Strength(Config));
    }
}
