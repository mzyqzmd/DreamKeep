using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>玩家回合结束阶段：进入即广播回合结束信号（操作窗口关闭），再结算灼烧与再生、弃掉未保留的手牌并衰减状态。</summary>
    public sealed class BP_PlayerTurnEnd : BattlePhase
    {
        public BP_PlayerTurnEnd(BattleLoop owner_) : base(owner_)
        {
        }

        public override BaseState<BattleLoop> Next => _owner.PhaseEnemyTurnStart;

        #region 阶段异步逻辑：关闭操作窗口 → 灼烧/再生 → 弃手牌/衰减，两批动作顺序严格保留

        public override async UniTask RunAsync()
        {
            await PlayEvent(new BE_PlayerTurnEnded());

            BattleContext context = _owner.BattleContext;
            Player player = context.Player;
            if (player.Creature.IsDead) return;

            List<Creature> self = new List<Creature> { player.Creature };
            ActionSystem actions = _owner.ActionSystem;

            // 回合末状态结算（灼烧 → 再生）：行为与顺序由触发管线统一给出，灼烧显式关闭数值修正
            List<BattleAction> triggered = new();
            HookTrigger.CollectTurnEnd(player.Creature, self, triggered);
            for (int i = 0; i < triggered.Count; i++)
                actions.Enqueue(triggered[i]);

            await actions.PerformAllAsync();

            // 弃手牌：未带「保留」的整手收成一批一次弃置，表现层整波同飞，全部落地才轮到衰减与敌方回合；
            // 倒序收集＝保持既有弃牌顺序（手牌末位那张先入堆）；筛选跑在任何动作执行之前，手牌不会被边遍历边改动
            CardPile hand = player.CombatState.Hand;
            List<CardModel> discardBatch = new();
            for (int i = hand.Count - 1; i >= 0; i--)
            {
                CardModel card = hand.Cards[i];
                if (card.IsRetain) continue;
                discardBatch.Add(card);
            }
            if (discardBatch.Count > 0) actions.Enqueue(new BA_DiscardBatch(player.Creature, self, discardBatch, hand));

            actions.Enqueue(new BA_DecayPerk(player.Creature, self, EPerkDecayTiming.TurnEnd));
            await actions.PerformAllAsync();
        }

        #endregion
    }
}
