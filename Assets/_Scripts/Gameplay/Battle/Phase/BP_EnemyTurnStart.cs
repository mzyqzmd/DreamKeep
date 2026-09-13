using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Gameplay
{
    /// <summary>敌人回合开始阶段：清空敌人上回合残留的格挡，为意图结算做准备。</summary>
    /// <remarks>敌人当前无获取格挡的途径时本循环短路；保留是为敌人防御意图预留。</remarks>
    public class BP_EnemyTurnStart : BattlePhase
    {
        public BP_EnemyTurnStart(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "敌人回合开始";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhaseEnemyTurnActive;

        #region 阶段异步逻辑：逐敌人清格挡后等待演出
        public override async UniTask RunAsync()
        {
            await PlayEvents(new BE_TurnPhaseChanged(PhaseName));
            ActionSystem actions = _owner.ActionSystem;

            foreach (Enemy enemy in _owner.BattleContext.Enemies)
            {
                if (enemy.Creature.IsDead) continue;
                List<Creature> self = new List<Creature> { enemy.Creature };
                actions.Enqueue(new BA_ClearBlock(enemy.Creature, self));
            }

            await actions.PerformAllAsync();
        }
        #endregion
    }
}
