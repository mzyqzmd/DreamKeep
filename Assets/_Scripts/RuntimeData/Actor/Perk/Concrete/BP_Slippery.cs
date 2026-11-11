using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「滑溜」运行时实例：层数即剩余的压伤次数，伤害上限恒为 1 点；
    /// 数值修正由 GameBattle 的 StatHook 分派表向本类取用，消费层数由掉血后的 SlipperyHook 执行。</summary>
    public sealed class BP_Slippery : BattlePerk
    {
        public BP_Slippery(SO_Slippery data_) : base(data_)
        {
        }

        /// <summary>上限贡献：宿主自己挨打且仍有层数时，本次伤害最多 1 点。</summary>
        public StatModifier GetModifier(EStatChannel channel_, Creature host_, Creature source_, Creature target_) =>
            channel_ == EStatChannel.Damage && host_ == target_ && Layers > 0
                ? new StatModifier(0, 1f, int.MinValue, 1)
                : StatModifier.Neutral;

        public override string GetDescription() =>
            $"下 {Layers} 次要失去生命值时，\n只会失去 1 点生命";

        public override BattlePerk Clone() => WithLayers(new BP_Slippery((SO_Slippery)_data));
    }
}