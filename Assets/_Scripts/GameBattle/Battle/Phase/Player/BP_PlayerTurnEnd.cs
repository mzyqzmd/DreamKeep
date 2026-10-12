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
            await PlayEvents(new BE_PlayerTurnEnded());

            BattleContext context = _owner.BattleContext;
            Player player = context.Player;
            if (player.Creature.IsDead) return;

            List<Creature> self = new List<Creature> { player.Creature };
            ActionSystem actions = _owner.ActionSystem;

            // 灼烧属受到伤害：无来源纯状态结算（Source=null 不触发任何人的攻击演出），可被格挡吸收，但不吃力量/虚弱/易伤修正
            int burn = player.Creature.GetPerkLayers<BP_Burn>();
            if (burn > 0)
                actions.Enqueue(new BA_Damage(null, self, burn));

            // 再生同灼烧：无来源纯状态结算，事件只带目标
            int regen = player.Creature.GetPerkLayers<BP_Regen>();
            if (regen > 0)
                actions.Enqueue(new BA_Heal(null, self, regen));

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
