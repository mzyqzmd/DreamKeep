using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>玩家回合进行阶段：唯一的等待型阶段，只接受命令，不自动推进。</summary>
    public class Phase_PlayerTurnActive : BattlePhase
    {
        public Phase_PlayerTurnActive(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "玩家回合进行";
        public override bool IsWaiting => true;
        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnEnd;

        public override void Enter() => _owner.Events.Emit(new TurnPhaseChangedEvent(PhaseName));

        #region 打出一张卡：校验能量与手牌后建队并同步排空
        public void PlayCard(int cardInstanceId_, int aimSlot_)
        {
            BattleState state = _owner.BattleState;
            InBattleCard card = state.Hand.Find(cardInstanceId_);
            if (card == null) return;
            if (state.Player.Energy < card.Cost.Value) return;

            List<Actor> self = new List<Actor> { state.Player };
            ActionSystem actions = _owner.ActionSystem;

            actions.Enqueue(new Action_SpendEnergy(state.Player, self, card.Cost.Value));
            _owner.AffixFactory.Build(state, card, state.Player, aimSlot_);
            // 消耗与遗忘已在队列内让卡离场，弃置动作取不到卡会自动跳过
            actions.Enqueue(new Action_Discard(state.Player, self, card.InstanceId));
            actions.PerformAll();

            _owner.Hooks.InvokePoint<HP_AfterCardPlayed>(state);
        }
        #endregion
    }
}
