using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>玩家回合进行阶段：唯一的等待型阶段，只接受命令，不自动推进。
    /// 进入本阶段即"玩家操作窗口开启"，但等待型阶段不被主循环驱动 RunAsync，故该信号由上一阶段 BP_PlayerTurnStart 在抽牌演出收尾处广播 BE_PlayerTurnActiveStarted。
    /// 打不出的命令（能量不足、牌不在手牌、带「不能被打出」词条）不静默丢弃，一律广播拒绝事件交由视图退牌。</summary>
    public sealed class BP_PlayerTurnActive : BattlePhase, IBattleCommandReceiver
    {
        public BP_PlayerTurnActive(BattleLoop owner_) : base(owner_)
        {
        }

        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnEnd;

        #region 阶段异步逻辑：等待型阶段不产生演出，主循环也不会驱动本方法（只分发 HandleCommand），保留空实现仅作入口钩子的位置说明

        public override UniTask RunAsync() => UniTask.CompletedTask;

        #endregion

        #region 命令分派：等待型阶段唯一处理玩家意图的入口，由 BattleLoop 在等待态取出命令后调用

        public async UniTask HandleCommand(BattleCommand command_)
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

        #region 打出一张卡：校验手牌、能量与「不能被打出」词条后建队并异步等待演出

        private async UniTask PlayCardAsync(CardModel card_, int aimSlot_)
        {
            BattleContext context = _owner.BattleContext;
            PlayerCombatState state = context.Player.CombatState;

            // 卡可能已在更早的队列里离手，不在手牌即视为命令失效；广播拒绝让视图把牌退回手牌
            if (!state.Hand.Contains(card_))
            {
                await PlayEvents(new BE_CardPlayRejected(card_));
                return;
            }

            if (state.CurrentEnergy < card_.Cost.Value)
            {
                await PlayEvents(new BE_CardPlayRejected(card_));
                return;
            }

            // 带「不能被打出」词条的牌一律拒绝：视图据此把牌从出牌区退回手牌原槽位
            if (card_.IsUnableToPlay)
            {
                await PlayEvents(new BE_CardPlayRejected(card_));
                return;
            }

            List<Creature> self = new List<Creature> { context.Player.Creature };

            // 入打出堆动作产出出牌事件，必须早于结算动作，否则播放队列会先播死亡再播攻击
            _owner.ActionSystem.Enqueue(new BA_ToPlayPile(context.Player.Creature, self, card_));
            _owner.ActionSystem.Enqueue(new BA_SpendEnergy(context.Player.Creature, self, card_.Cost.Value));
            foreach (BattleAction action in CardAffixResolver.Build(context, card_, context.Player, aimSlot_))
                _owner.ActionSystem.Enqueue(action);
            // 消耗与遗忘已在队列内让卡离场，弃置动作取不到卡会自动跳过
            _owner.ActionSystem.Enqueue(new BA_DiscardSingle(context.Player.Creature, self, card_, state.PlayPile));
            await _owner.ActionSystem.PerformAllAsync();
        }

        #endregion
    }
}