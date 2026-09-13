namespace _DreamKeep.RuntimeData
{
    /// <summary>复合意图：最多两条子意图，执行时按顺序依次展开。</summary>
    public class EI_Mixed : EnemyIntent
    {
        public EnemyIntent First { get; }
        public EnemyIntent Second { get; }

        public EI_Mixed(EnemyIntent first_, EnemyIntent second_)
        {
            First = first_;
            Second = second_;
        }
    }
}
