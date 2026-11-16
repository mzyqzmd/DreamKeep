using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人被击晕时的演出事件：横冲直撞打断与失衡自击晕都会抛出（两种施加者见 BA_RampageBreak / BA_ImbalancedStun），
    /// 表现层据此播 stun → stun_loop 击晕链。</summary>
    public struct BE_EnemyStunned : IBattleEvent
    {
        public Enemy Enemy { get; }

        public BE_EnemyStunned(Enemy enemy_) => Enemy = enemy_;
    }
}
