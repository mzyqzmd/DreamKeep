using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>玩家回合开始阶段：清残留格挡、结算回合开始的状态、按能量上限回满能量，并逐张抽牌；
    /// 抽牌演出收尾处广播 BE_PlayerTurnActiveStarted，作为"玩家操作窗口开启"的唯一信号（等待型进行阶段不会被主循环驱动 RunAsync，入口信号只能在此发出）。</summary>
    public sealed class BP_PlayerTurnStart : BattlePhase
    {
        public BP_PlayerTurnStart(BattleLoop owner_) : base(owner_)
        {
        }

        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnActive;

        #region 阶段异步逻辑：建队 + 逐动作等待演出

        public override async UniTask RunAsync()
        {
            await PlayEvent(new BE_TurnStarted(_owner.BattleContext.TurnCount, true));

            Player player = _owner.BattleContext.Player;
            List<Creature> self = new List<Creature> { player.Creature };
            ActionSystem actions = _owner.ActionSystem;

            // 先清盾再结算：回合开始获得的格挡不会被本阶段清掉，上回合残留的格挡也不跨回合保留
            actions.Enqueue(new BA_ClearBlock(player.Creature, self));
            actions.Enqueue(new BA_SettlePerk(player.Creature, self, EPerkSettleTiming.TurnStart));
            actions.Enqueue(new BA_ResetEnergy(player.Creature, self, player.CombatState.MaxEnergy));
            for (int i = 0; i < _owner.DrawPerTurn; i++)
                actions.Enqueue(new BA_DrawCard(player.Creature, self));
            await actions.PerformAllAsync();

            // 抽牌全部落稳才开窗：此后主循环立刻推进到等待型进行阶段，除本阶段收尾外没有别的时机能发出该信号
            await PlayEvent(new BE_PlayerTurnActiveStarted());
        }

        #endregion
    }
}
