using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>玩家回合进行阶段：唯一的等待型阶段，只接受命令，不自动推进。
    /// 进入本阶段即"玩家操作窗口开启"，但等待型阶段不被主循环驱动 RunAsync，故该信号由上一阶段 BP_PlayerTurnStart 在抽牌演出收尾处广播 BE_PlayerTurnActiveStarted。
    /// 打不出的命令（能量不足、牌不在手牌、带「不能被打出」词条、昏眩下本回合已打满 1 张）不静默丢弃，一律广播拒绝事件交由视图退牌。</summary>
    public sealed class BP_PlayerTurnActive : BattlePhase, IBattleCommandReceiver
    {
        public BP_PlayerTurnActive(BattleLoop owner_) : base(owner_)
        {
        }

        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnEnd;

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
                case BC_UsePotion usePotion:
                    await UsePotionAsync(usePotion.SlotIndex, usePotion.AimSlot);
                    break;
            }
        }

        #endregion

        #region 使用药水：校验槽位与落点后先清槽再结算，顺序与出牌一致（清槽先于效果）

        private async UniTask UsePotionAsync(int slotIndex_, int aimSlot_)
        {
            BattleContext context = _owner.BattleContext;
            Player player = context.Player;
            if (slotIndex_ < 0 || slotIndex_ >= player.PotionSlots.Count) return;

            SO_Potion potion = player.PotionSlots[slotIndex_];
            if (potion == null) return;

            // 效果先建队：落点非法（单体没有合法目标）时整瓶都不结算，不能先清槽再失败。
            // 落点随动作一起带出去：离槽事件据此让表现层把药水扔向同一批目标，视图不重算规则
            List<BattleAction> effects = PotionResolver.Build(context, potion, player, aimSlot_, out List<Creature> targets);
            if (effects.Count == 0) return;

            _owner.ActionSystem.Enqueue(new BA_ConsumePotion(player.Creature, targets, slotIndex_));
            foreach (BattleAction action in effects) _owner.ActionSystem.Enqueue(action);
            await _owner.ActionSystem.PerformAllAsync();
        }

        #endregion

        #region 打出一张卡：校验手牌、能量、「不能被打出」词条与昏眩限牌后建队并异步等待演出

        private async UniTask PlayCardAsync(CardModel card_, int aimSlot_)
        {
            BattleContext context = _owner.BattleContext;
            PlayerCombatState state = context.Player.CombatState;

            // 四种打不出的情形一律广播拒绝，让视图把牌从出牌区退回手牌原槽位：
            // 卡可能已在更早的队列里离手、能量不足、带「不能被打出」词条、昏眩下本回合已打满 1 张
            if (TryGetRejectReason(context, card_, out ECardPlayRejectReason reason))
            {
                await PlayEvent(new BE_CardPlayRejected(card_, reason));
                return;
            }

            Creature playerCreature = context.Player.Creature;
            List<Creature> self = new List<Creature> { playerCreature };

            // 入打出堆动作产出出牌事件，必须早于结算动作，否则播放队列会先播死亡再播攻击
            _owner.ActionSystem.Enqueue(new BA_ToPlayPile(playerCreature, self, card_));
            _owner.ActionSystem.Enqueue(new BA_SpendEnergy(playerCreature, self, card_.Cost.Value));

            // 超巨化与复制都在建队时查一次、按命中入队消费，不在动作里反复判定：
            // 倍率经 StatHook 在伤害求值时生效，消费动作排在本次词条结算之后，预览与实扣同口径
            foreach (BattleAction action in CardAffixResolver.Build(context, card_, context.Player, aimSlot_, false))
                _owner.ActionSystem.Enqueue(action);
            ConsumePlayOncePerks(context, card_, playerCreature, self, aimSlot_);

            // 消耗与遗忘已在队列内让卡离场，弃置动作取不到卡会自动跳过
            _owner.ActionSystem.Enqueue(new BA_DiscardSingle(playerCreature, self, card_, state.PlayPile));
            // 链尾才广播出牌：本张牌的词条已按旧层数结算，卡面预览与实扣一致
            _owner.ActionSystem.Enqueue(new BA_TickCardPlayed(playerCreature));
            await _owner.ActionSystem.PerformAllAsync();
        }

        // 一次性出牌状态：超巨化仅攻击牌命中，复制让本次词条再结算一遍且不重复扣能量与入堆
        private void ConsumePlayOncePerks(BattleContext context_, CardModel card_, Creature playerCreature_,
            List<Creature> self_, int aimSlot_)
        {
            BattlePerk gigantification = BattlePerkQuery.Find<BP_Gigantification>(playerCreature_);
            if (gigantification != null && HasDamageAffix(card_))
                _owner.ActionSystem.Enqueue(new BA_ConsumePerk(playerCreature_, self_, gigantification));

            BattlePerk duplication = BattlePerkQuery.Find<BP_Duplication>(playerCreature_);
            if (duplication == null) return;

            foreach (BattleAction action in CardAffixResolver.Build(context_, card_, context_.Player, aimSlot_, false))
                _owner.ActionSystem.Enqueue(action);
            _owner.ActionSystem.Enqueue(new BA_ConsumePerk(playerCreature_, self_, duplication));
        }

        private static bool HasDamageAffix(CardModel card_)
        {
            foreach (CardAffix affix in card_.ActiveAffixes)
                if (affix is CA_Damage) return true;
            return false;
        }

        // 能否打出与为什么打不出同源：判定顺序即拒绝原因优先级，四种情形必居其一
        private static bool TryGetRejectReason(BattleContext context_, CardModel card_,
            out ECardPlayRejectReason reason_)
        {
            PlayerCombatState state = context_.Player.CombatState;
            if (!state.Hand.Contains(card_)) reason_ = ECardPlayRejectReason.NotInHand;
            else if (state.CurrentEnergy < card_.Cost.Value) reason_ = ECardPlayRejectReason.NotEnoughEnergy;
            else if (card_.IsUnableToPlay) reason_ = ECardPlayRejectReason.Unplayable;
            else if (IsDizzy(context_) && state.CardsPlayedThisTurn >= 1)
                reason_ = ECardPlayRejectReason.DizzyLimited;
            else
            {
                reason_ = default;
                return false;
            }

            return true;
        }

        // 宿主身上是否挂着昏眩：只在出牌校验时查一次，不缓存
        private static bool IsDizzy(BattleContext context_)
        {
            IReadOnlyList<BattlePerk> perks = context_.Player.Creature.Perks;
            for (int i = 0; i < perks.Count; i++)
                if (perks[i] is BP_Dizzy) return true;

            return false;
        }

        #endregion
    }
}
