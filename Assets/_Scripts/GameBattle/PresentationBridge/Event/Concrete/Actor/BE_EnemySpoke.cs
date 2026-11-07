using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人出招台词：执行意图前广播，表现层在敌人头顶弹 R 气泡；
    /// 处理器同步完成、不阻塞战斗，未配置台词时根本不发。</summary>
    public struct BE_EnemySpoke : IBattleEvent
    {
        public Enemy Enemy { get; }
        public string Line { get; }

        public BE_EnemySpoke(Enemy enemy_, string line_)
        {
            Enemy = enemy_;
            Line = line_;
        }
    }
}
