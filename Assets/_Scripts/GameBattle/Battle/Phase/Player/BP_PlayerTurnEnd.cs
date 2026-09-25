using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>玩家回合结束阶段：结算灼烧与再生，弃掉未保留的手牌并衰减状态。</summary>
    public class BP_PlayerTurnEnd : BattlePhase
    {
        public BP_PlayerTurnEnd(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override BaseState<BattleLoop> Next => _owner.PhaseEnemyTurnStart;

        #region 阶段异步逻辑：灼烧/再生 → 弃手牌/衰减，两批动作顺序严格保留

        public override async UniTask RunAsync()
        {
            BattleContext context = _owner.BattleContext;
            Player player = context.Player;
            if (player.Creature.IsDead) return;

            List<Creature> self = new List<Creature> { player.Creature };
            ActionSystem actions = _owner.ActionSystem;

            // 灼烧属受到伤害：可被格挡吸收，但不吃力量/虚弱/易伤修正
            int burn = player.Creature.GetPerkLayers<BP_Burn>();
            if (burn > 0)
                actions.Enqueue(new BA_Damage(player.Creature, self, burn));

            int regen = player.Creature.GetPerkLayers<BP_Regen>();
            if (regen > 0)
                actions.Enqueue(new BA_Heal(player.Creature, self, regen));

            await actions.PerformAllAsync();

            // 弃手牌：未带「保留」的逐张建动作，逐个走完"离手 → 飞向弃牌堆 → 落地"，落地时计数才递增
            // 倒序建动作＝保持既有弃牌顺序（手牌末位那张先飞）；筛选跑在任何动作执行之前，手牌不会被边遍历边改动
            CardPile hand = player.CombatState.Hand;
            for (int i = hand.Count - 1; i >= 0; i--)
            {
                CardModel card = hand.Cards[i];
                if (card.IsRetain) continue;
                actions.Enqueue(new BA_Discard(player.Creature, self, card, hand));
            }

            actions.Enqueue(new BA_DecayPerk(player.Creature, self, EPerkDecayTiming.TurnEnd));
            await actions.PerformAllAsync();
        }

        #endregion
    }
}
