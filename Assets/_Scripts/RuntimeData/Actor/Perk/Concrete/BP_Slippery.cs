using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「滑溜」运行时实例：层数即剩余的压伤次数，伤害上限恒为 1 点；
    /// 伤害钳制由 GameBattle 的 SlipperyHook.Clamp 单点执行，本类只持层数并供掉血后消费。</summary>
    public sealed class BP_Slippery : BattlePerk
    {
        public BP_Slippery(SO_Slippery data_) : base(data_)
        {
        }

        public override string GetDescription() =>
            $"下 {Layers} 次要失去生命值时，\n只会失去 1 点生命";

        public override BattlePerk Clone() => WithLayers(new BP_Slippery((SO_Slippery)_data));
    }
}