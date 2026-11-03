namespace _DreamKeep.ConfigData
{
    /// <summary>遗物稀有度：展示与将来抽取分桶的数据维度，不参与战斗逻辑。</summary>
    public enum ERelicRarity
    {
        Starter,
        Common,
        Rare,
        Shop,

        /// <summary>远古：先古之民专属发放，不参与常规掉落（涅奥池等）。</summary>
        Ancient,
    }
}
