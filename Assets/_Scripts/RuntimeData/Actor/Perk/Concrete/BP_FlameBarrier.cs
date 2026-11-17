using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「火焰屏障」运行时实例：层数即受击时对攻击者造成的基础伤害，回合结束自动移除。</summary>
    public sealed class BP_FlameBarrier : BattlePerk
    {
        private SO_FlameBarrier Config => (SO_FlameBarrier)_data;

        public BP_FlameBarrier(SO_FlameBarrier data_) : base(data_) { }

        /// <summary>层数是反伤数值而非持续回合：回合结束整条移除，不按层递减。</summary>
        public override bool Settle() => true;

        public override string GetDescription() => GetDescription(null, Layers);

        public override string GetDescription(IStatRenderer valueRenderer_, int baseLayers_) =>
            $"本回合每受到一次攻击，\n对攻击者造成 {RenderLayers(valueRenderer_, baseLayers_)} 点伤害";

        public override BattlePerk Clone() => WithLayers(new BP_FlameBarrier(Config));
    }
}
