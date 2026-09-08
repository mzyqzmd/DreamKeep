using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>运行时状态基类：状态仅为可施加的数据（层数），具体行为由结算管线解释。</summary>
    public abstract class InBattlePerk
    {
        protected InBattlePerk(int layers_) => Layers = layers_;

        /// <summary>回合结束是否自动衰减一层；力量等永久状态覆写为 false。</summary>
        public virtual bool DecaysEachTurn => true;
        public int Layers { get; private set; }
        public void AddLayers(int amount_) => Layers += amount_;

        public abstract string Name { get; }
        public abstract Sprite Icon { get; }
        public abstract string GetDescription();
        public abstract InBattlePerk Clone();
    }
}
