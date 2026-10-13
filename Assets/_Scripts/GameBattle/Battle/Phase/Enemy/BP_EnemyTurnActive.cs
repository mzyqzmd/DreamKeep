using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人回合进行阶段：按槽位从左到右逐个敌人执行其展示意图（站最前槽位自动替换为最前槽位意图），执行完推进到下一条并广播，供表现层预告。</summary>
    public sealed class BP_EnemyTurnActive : BattlePhase
    {
        public BP_EnemyTurnActive(BattleLoop owner_) : base(owner_)
        {
        }

        public override BaseState<BattleLoop> Next => _owner.PhaseEnemyTurnEnd;

        #region 阶段异步逻辑：逐敌人取展示意图 → 转动作入队 → 等待演出 → 推进并广播下一条

        public override async UniTask RunAsync()
        {
            BattleContext context = _owner.BattleContext;
            ActionSystem actions = _owner.ActionSystem;

            foreach (Enemy enemy in context.GetAliveEnemies())
            {
                EnemyIntent intent = enemy.PreviewIntent;
                if (intent != null)
                {
                    foreach (BattleAction action in EnemyIntentResolver.Build(context, enemy, intent))
                        actions.Enqueue(action);
                    await actions.PerformAllAsync();
                }

                enemy.AdvanceIntent();
                if (enemy.Creature.IsDead) continue;
                await PlayEvent(new BE_EnemyIntentChanged(enemy, enemy.PreviewIntent));
            }
        }

        #endregion
    }
}
