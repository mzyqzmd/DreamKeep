using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「复制」运行时实例：下一张牌额外打出一次，触发后整条移除；未触发则回合结束失效。</summary>
    public sealed class BP_Duplication : BattlePerk
    {
        private SO_Duplication Config => (SO_Duplication)_data;

        public BP_Duplication(SO_Duplication data_) : base(data_) { }

        public override string GetDescription() => "下一张打出的牌\n额外打出一次";

        public override BattlePerk Clone() => WithLayers(new BP_Duplication(Config));
    }
}
