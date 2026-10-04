namespace _DreamKeep.RuntimeData
{
    /// <summary>运行时敌人意图：由配置在装配期烘焙而来，只描述"这一回合要做什么"，不持有任何配置引用。</summary>
    public abstract class EnemyIntent
    {
        /// <summary>意图说明：直接给表现层弹提示用，数值取自运行时真值。</summary>
        public abstract string GetDescription();
    }
}
