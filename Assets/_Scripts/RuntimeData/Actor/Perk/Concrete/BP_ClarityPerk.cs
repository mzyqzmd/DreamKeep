using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「明晰」运行时实例：层数是剩余回合数，每回合开始时额外抽 1 张牌并递减一层。</summary>
    public sealed class BP_ClarityPerk : BattlePerk
    {
        private SO_ClarityPerk Config => (SO_ClarityPerk)_data;

        public BP_ClarityPerk(SO_ClarityPerk data_) : base(data_) { }

        public override string GetDescription() => $"接下来 {Layers} 个回合开始时\n额外抽 1 张牌";

        public override BattlePerk Clone() => WithLayers(new BP_ClarityPerk(Config));
    }
}
