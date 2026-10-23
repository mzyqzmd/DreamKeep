using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人意图变化：战斗开始与每次行动后广播其下一条意图，供表现层做意图预告。</summary>
    public struct BE_EnemyIntentChanged : IBattleEvent
    {
        public Enemy Enemy { get; }
        public EnemyIntent Intent { get; }

        public BE_EnemyIntentChanged(Enemy enemy_, EnemyIntent intent_)
        {
            Enemy = enemy_;
            Intent = intent_;
        }
    }
}
