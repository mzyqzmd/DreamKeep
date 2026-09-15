using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人回合结束阶段：敌人状态衰减并推进回合计数，一回合到此结束。</summary>
    public class BP_EnemyTurnEnd : BattlePhase
    {
        public BP_EnemyTurnEnd(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "敌人回合结束";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnStart;

        #region 阶段异步逻辑：逐敌人衰减状态后等待演出，再推进回合计数
        public override async UniTask RunAsync()
        {
            await PlayEvents(new BE_TurnPhaseChanged(PhaseName));
            BattleContext context = _owner.BattleContext;
            ActionSystem actions = _owner.ActionSystem;

            foreach (Enemy enemy in context.Enemies)
            {
                if (enemy.Creature.IsDead) continue;
                List<Creature> self = new List<Creature> { enemy.Creature };
                actions.Enqueue(new BA_DecayPerk(enemy.Creature, self));
            }

            await actions.PerformAllAsync();

            // 唯一例外：回合计数无来源无目标且不发事件，不做成动作，直接推进
            context.TurnCount++;
        }
        #endregion
    }
}
