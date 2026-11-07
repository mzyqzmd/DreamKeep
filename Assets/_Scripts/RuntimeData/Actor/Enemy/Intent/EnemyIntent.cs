namespace _DreamKeep.RuntimeData
{
    /// <summary>运行时敌人意图：由配置在装配期烘焙而来，只描述"这一回合要做什么"，不持有任何配置引用；
    /// SpeechLine 是随配置带来的出招台词，null/空表示这一招沉默。</summary>
    public abstract class EnemyIntent
    {
        public string SpeechLine { get; internal set; }

        public abstract string GetIntentName();

        /// <summary>意图说明：直接给表现层弹提示用的静态语义句，不含数值。</summary>
        public abstract string GetDescription();
    }
}
