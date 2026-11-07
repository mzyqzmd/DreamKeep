using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>横冲直撞触发的击晕演出：仅状态打断（自己触发的击晕）时抛出，位移撞晕等玩家造成的击晕不发，
    /// 表现层据此播 stun → stun_loop 击晕链。</summary>
    public struct BE_EnemyStunned : IBattleEvent
    {
        public Enemy Enemy { get; }

        public BE_EnemyStunned(Enemy enemy_) => Enemy = enemy_;
    }
}
