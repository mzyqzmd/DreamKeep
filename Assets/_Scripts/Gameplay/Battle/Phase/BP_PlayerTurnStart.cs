using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Gameplay
{
    /// <summary>玩家回合开始阶段：清残留格挡、按能量上限回满能量，并逐张抽牌。</summary>
    public class BP_PlayerTurnStart : BattlePhase
    {
        public BP_PlayerTurnStart(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "玩家回合开始";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnActive;

        #region 阶段异步逻辑：建队 + 逐动作等待演出

        public override async UniTask RunAsync()
        {
            await PlayEvents(new BE_TurnPhaseChanged(PhaseName));

            Player player = _owner.BattleContext.Player;
            List<Creature> self = new List<Creature> { player.Creature };
            ActionSystem actions = _owner.ActionSystem;

            // 先落盾再回能：玩家上回合残留的格挡不跨回合保留
            actions.Enqueue(new BA_ClearBlock(player.Creature, self));
            actions.Enqueue(new BA_ResetEnergy(player.Creature, self, player.CombatState.MaxEnergy));
            for (int i = 0; i < _owner.DrawPerTurn; i++)
                actions.Enqueue(new BA_DrawCard(player.Creature, self));
            await actions.PerformAllAsync();
        }

        #endregion
    }
}
