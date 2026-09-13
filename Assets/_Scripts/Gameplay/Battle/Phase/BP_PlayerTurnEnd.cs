using System.Collections.Generic;
using _DreamKeep.Data;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Gameplay
{
    /// <summary>玩家回合结束阶段：结算灼烧与再生，弃掉未保留的手牌并衰减状态。</summary>
    public class BP_PlayerTurnEnd : BattlePhase
    {
        public BP_PlayerTurnEnd(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "玩家回合结束";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhaseEnemyTurnStart;

        #region 阶段异步逻辑：灼烧/再生 → 弃手牌/衰减，两批动作顺序严格保留

        public override async UniTask RunAsync()
        {
            await PlayEvents(new BE_TurnPhaseChanged(PhaseName));
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

            actions.Enqueue(new BA_DiscardHand(player.Creature, self));
            actions.Enqueue(new BA_DecayPerk(player.Creature, self));
            await actions.PerformAllAsync();
        }

        #endregion
    }
}
