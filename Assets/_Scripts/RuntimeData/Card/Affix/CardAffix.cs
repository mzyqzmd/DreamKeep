namespace _DreamKeep.RuntimeData
{
    /// <summary>运行时词条基类：由配置数据烘焙而来，是卡组原型与战斗副本共用的词条形态，不持有任何配置引用。</summary>
    public abstract class CardAffix
    {
        /// <summary>结算管线权重，越小越先结算；修饰型词条恒为 -1，不进入效果管线。</summary>
        internal abstract int PipelineOrder { get; }
        /// <summary>词条名称。</summary>
        public abstract string Name { get; }
        /// <summary>卡面是否显示描述；为 false 时由表现层生成 tip。</summary>
        public abstract bool ShowDescription { get; }

        /// <summary>逐条描述：数值型词条把基础值交给渲染器，不感知修正与颜色；渲染器为 null 即按原值纯文本输出。</summary>
        public abstract string GetDescription(IStatRenderer valueRenderer_ = null);

        #region 深拷贝一份独立词条，供战斗卡与「给予」使用，避免多张卡共享同一实例
        public abstract CardAffix Clone();
        #endregion
    }
}
