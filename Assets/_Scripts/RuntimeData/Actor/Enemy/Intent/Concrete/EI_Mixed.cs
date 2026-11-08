
namespace _DreamKeep.RuntimeData
{
    /// <summary>复合意图：最多两条子意图，执行时按顺序依次展开。</summary>
    public sealed class EI_Mixed : EnemyIntent
    {
        public EnemyIntent First { get; }
        public EnemyIntent Second { get; }

        public EI_Mixed(EnemyIntent first_, EnemyIntent second_)
        {
            First = first_;
            Second = second_;
        }

        public override string GetIntentName() => "能看到这个意图说明游戏有bug";

        // 混合自身不展示 tip（表现层会把子意图逐条展开），仅为满足抽象契约拼一份子描述
        public override string GetDescription() => "能看到这个意图说明游戏有bug";
    }
}