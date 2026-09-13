using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

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

        #region 阶段异步逻辑：等待型阶段无动作队列，仅派发阶段变更事件
        public override async UniTask RunAsync()
        {
            await PlayEvents(new BE_TurnPhaseChanged(PhaseName));
            await base.RunAsync();
        }
        #endregion

        #region 打出一张卡：校验能量与手牌后建队并异步等待演出
        public async UniTask PlayCardAsync(CardModel card_, int aimSlot_)
        {
            BattleContext context = _owner.BattleContext;
            PlayerCombatState state = context.Player.CombatState;

            // 卡可能已在更早的队列里离手，不在手牌即视为命令失效
            if (!state.Hand.Contains(card_)) return;
            if (state.CurrentEnergy < card_.Cost.Value) return;

            List<Creature> self = new List<Creature> { context.Player.Creature };
            ActionSystem actions = _owner.ActionSystem;

            // TODO 诅咒牌禁止打出的校验：基于 card_.Type == ECardType.Curse 判定，逻辑留待后续实现
            // 入打出堆动作产出出牌事件，必须早于结算动作，否则播放队列会先播死亡再播攻击
            actions.Enqueue(new BA_ToPlayPile(context.Player.Creature, self, card_));
            actions.Enqueue(new BA_SpendEnergy(context.Player.Creature, self, card_.Cost.Value));
            foreach (BattleAction action in _owner.AffixResolver.Build(context, card_, context.Player, aimSlot_))
                actions.Enqueue(action);
            // 消耗与遗忘已在队列内让卡离场，弃置动作取不到卡会自动跳过
            actions.Enqueue(new BA_Discard(context.Player.Creature, self, card_));
            await actions.PerformAllAsync();
        }
        #endregion

        #region 命令分派：等待型阶段唯一处理玩家意图的入口，由 BattleLoop 在等待态取出命令后调用
        public override async UniTask HandleCommand(BattleCommand command_)
        {
            switch (command_)
            {
                case BC_PlayCard playCard:
                    await PlayCardAsync(playCard.PlayedCard, playCard.AimSlot);
                    break;
                case BC_PlayerEndTurn _:
                    _owner.EndPlayerTurn();
                    break;
            }
        }
        #endregion
    }
}
