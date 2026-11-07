using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>击晕结束（醒来）演出：敌人回合消费击晕时抛出，表现层据此播 wake_up 回待机。</summary>
    public struct BE_EnemyStunEnded : IBattleEvent
    {
        public Enemy Enemy { get; }

        public BE_EnemyStunEnded(Enemy enemy_) => Enemy = enemy_;
    }
}
