using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人回合进行阶段：按槽位从左到右逐个敌人执行其展示意图（击晕优先、其次站最前槽位自动替换为最前槽位意图），
    /// 执行完推进到下一条并广播，供表现层预告；被击晕的敌人整回合跳过且不推进意图。
    /// 台词宣告已前移到 BP_EnemyTurnStart（回合开始即宣告本回合准备行动），本阶段只管动作。</summary>
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
                // 击晕：整回合跳过，执行完清除击晕即恢复原本意图（不推进意图序列）；醒来事件先行供表现层播 wake_up
                if (enemy.IsStunned)
                {
                    enemy.ConsumeStun();
                    await PlayEvent(new BE_EnemyStunEnded(enemy));
                    await PlayEvent(new BE_EnemyIntentChanged(enemy, enemy.PreviewIntent));
                    continue;
                }

                EnemyIntent intent = enemy.PreviewIntent;
                if (intent != null)
                {
                    foreach (BattleAction action in EnemyIntentResolver.Build(context, enemy, intent))
                        actions.Enqueue(action);
                    await actions.PerformAllAsync();
                }

                // 阶段一意图执行完即整表切二阶段：索引归零后不能再推进，否则二阶段从第 2 条开始
                if (enemy.ShouldSwitchPhaseAfterIntent)
                {
                    enemy.SwitchToPhaseTwo();
                    if (enemy.Creature.IsDead) continue;
                    await PlayEvent(new BE_EnemyIntentChanged(enemy, enemy.PreviewIntent));
                    continue;
                }

                enemy.AdvanceIntent();
                if (enemy.Creature.IsDead) continue;
                await PlayEvent(new BE_EnemyIntentChanged(enemy, enemy.PreviewIntent));
            }
        }

        #endregion
    }
}
