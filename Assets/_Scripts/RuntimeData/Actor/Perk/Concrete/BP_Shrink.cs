using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「缩小」运行时实例：降低宿主造成的伤害，并记录施加者供其死亡时精确解除。
    /// 比率在烘焙时从定义资产拷进本实例，之后流程若要改值改的是实例，定义资产永不被写。</summary>
    public sealed class BP_Shrink : BattlePerk
    {
        private readonly float _ratio;

        private SO_Shrink Config => (SO_Shrink)_data;

        public BP_Shrink(SO_Shrink data_) : base(data_) => _ratio = data_.Ratio;

        private BP_Shrink(SO_Shrink data_, float ratio_, Creature source_) : base(data_)
        {
            _ratio = ratio_;
            Source = source_;
        }

        /// <summary>施加者：甲虫死亡时按它精确清理，药水链路天然是玩家。</summary>
        public Creature Source { get; private set; }

        /// <summary>写入施加者：只认首次写入，重复施加不覆盖，避免多来源时丢失原解除条件。</summary>
        public void SetSource(Creature source_) => Source ??= source_;

        // float 无法精确表示 0.7，10 × 0.7f 会被取整管线截成 6；补偿到刚好跨过整数边界
        private const float MultiplierGuard = 1e-5f;

        /// <summary>乘性贡献：宿主自己出手时减为 ×(1−ratio)。</summary>
        public StatModifier GetModifier(EStatChannel channel_, Creature host_, Creature source_, Creature target_) =>
            channel_ == EStatChannel.Damage && host_ == source_
                ? new StatModifier(0, 1f - _ratio + MultiplierGuard)
                : StatModifier.Neutral;

        public override string GetDescription() =>
            $"接下来 {Layers} 回合内，造成的\n伤害 - {_ratio * 100:0} %，施加者死亡时解除";

        public override BattlePerk Clone() => WithLayers(new BP_Shrink(Config, _ratio, Source));
    }
}
