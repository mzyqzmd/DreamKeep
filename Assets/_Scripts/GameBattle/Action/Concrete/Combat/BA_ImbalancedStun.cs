using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>失衡击晕动作：施加击晕并广播（不切阶段、不移除状态）；由 FullBlockHook 在伤害落地的同一次结算里嵌套执行（只合事件不进队列）。</summary>
    public sealed class BA_ImbalancedStun : BattleAction
    {
        public BA_ImbalancedStun(Creature source_) : base(source_, new List<Creature> { source_ }) { }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();

            Enemy enemy = FindEnemy(battleContext_);
            if (enemy == null || enemy.IsStunned) return events;

            enemy.ApplyStun();
            events.Add(new BE_EnemyStunned(enemy));
            events.Add(new BE_EnemyIntentChanged(enemy, enemy.PreviewIntent));

            return events;
        }

        // 由 Creature 反查所属敌人：失衡只存在于敌人身上，找不到即数据异常，放弃后续演出
        private Enemy FindEnemy(BattleContext battleContext_)
        {
            IReadOnlyList<Enemy> enemies = battleContext_.Enemies;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i].Creature == Source) return enemies[i];

            return null;
        }
    }
}