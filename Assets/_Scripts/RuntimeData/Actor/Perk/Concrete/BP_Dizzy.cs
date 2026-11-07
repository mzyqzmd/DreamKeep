using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「昏眩」运行时实例：层数即持续的玩家回合数，宿主回合结束递减一层、归零移除；
    /// 出牌上限的拦截住在 GameBattle 的 BP_PlayerTurnActive，本类只携带数据与文案。</summary>
    public sealed class BP_Dizzy : BattlePerk
    {
        public BP_Dizzy(SO_Dizzy data_) : base(data_) { }

        public override string GetDescription() =>
            Layers > 1
                ? $"本回合你只能打出 <color=yellow>1</color> 张牌<br>效果还剩 {Layers} 个回合"
                : "本回合你只能打出 <color=yellow>1</color> 张牌";

        public override BattlePerk Clone() => WithLayers(new BP_Dizzy((SO_Dizzy)_data));
    }
}
