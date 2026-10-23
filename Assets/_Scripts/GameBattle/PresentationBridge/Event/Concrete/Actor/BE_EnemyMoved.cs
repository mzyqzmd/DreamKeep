using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人槽位移动：前进一格或最前槽位回绕归位完成后广播，供表现层做位移补间。</summary>
    public struct BE_EnemyMoved : IBattleEvent
    {
        public Enemy Enemy { get; }
        public int FromSlot { get; }
        public int ToSlot { get; }

        public BE_EnemyMoved(Enemy enemy_, int fromSlot_, int toSlot_)
        {
            Enemy = enemy_;
            FromSlot = fromSlot_;
            ToSlot = toSlot_;
        }
    }
}
