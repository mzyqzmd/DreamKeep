using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>玩家回合进行阶段：唯一的等待型阶段，只接受命令，不自动推进。</summary>
    public class BP_PlayerTurnActive : BattlePhase
    {
        public BP_PlayerTurnActive(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "玩家回合进行";
        public override bool IsWaiting => true;
        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnEnd;

        public override void Enter() => _owner.Events.Emit(new IBE_TurnPhaseChanged(PhaseName));

        #region 打出一张卡：校验能量与手牌后建队并同步排空
        public void PlayCard(CardModel card_, int aimSlot_)
        {
            BattleState state = _owner.BattleState;
            // 卡可能已在更早的队列里离手，不在手牌即视为命令失效
            if (!state.Hand.Contains(card_)) return;
            if (state.Player.Energy < card_.Cost.Value) return;

            List<Actor> self = new List<Actor> { state.Player };
            ActionSystem actions = _owner.ActionSystem;

            // TODO 诅咒牌禁止打出的校验：运行时卡模型不带卡面类型，需先给 CardModel 补类型字段
            // 出牌事件必须早于结算动作，否则播放队列会先播死亡再播攻击
            _owner.Events.Emit(new IBE_CardPlayed(card_));

            actions.Enqueue(new BA_SpendEnergy(state.Player, card_.Cost.Value));
            _owner.AffixFactory.Build(state, card_, state.Player, aimSlot_);
            // 消耗与遗忘已在队列内让卡离场，弃置动作取不到卡会自动跳过
            actions.Enqueue(new BA_Discard(state.Player, self, card_));
            actions.PerformAll();

            _owner.Hooks.InvokePoint<HP_AfterCardPlayed>(state);
        }
        #endregion
    }
}
