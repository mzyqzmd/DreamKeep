using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「横冲直撞」运行时实例：层数即血量阈值，不随回合衰减；
    /// 判定与触发都住在 GameBattle 的 HpDropHook / BA_RampageBreak，本类只携带数据与文案。</summary>
    public sealed class BP_Rampage : BattlePerk
    {
        public BP_Rampage(SO_Rampage data_) : base(data_) { }

        public override string GetDescription() =>
            $"生命值 <= {Layers} 时受到伤害：被击晕并结束本状态，<br>力量重置为 0，行动进入阶段二";

        public override BattlePerk Clone() => WithLayers(new BP_Rampage((SO_Rampage)_data));
    }
}
