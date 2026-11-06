namespace _DreamKeep.Utility
{
    /// <summary>候选件与其权重：多池合并抽取时每件候选携带所属池的档位权重，供加权轮盘抽样使用。</summary>
    public readonly struct WeightedItem<T>
    {
        /// <summary>候选件本体。</summary>
        public T Item { get; }

        /// <summary>相对权重：≤0 视为不参与抽取。</summary>
        public float Weight { get; }

        public WeightedItem(T item_, float weight_)
        {
            Item = item_;
            Weight = weight_;
        }
    }
}