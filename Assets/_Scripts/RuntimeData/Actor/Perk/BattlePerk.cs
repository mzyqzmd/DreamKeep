using _DreamKeep.ConfigData;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>战斗状态运行时实例：挂在生物身上、层数可变的独立副本，参数与显示名转发自配置模板（PerkData）；
    /// 数值修正与触发行为住在 GameBattle 的行为类里，本类不含逻辑。</summary>
    public abstract class BattlePerk
    {
        protected readonly PerkData _data;

        protected BattlePerk(PerkData data_) => _data = data_;

        /// <summary>当前层数：实例私有状态，施加与衰减都只改它。</summary>
        public int Layers { get; private set; }

        public Sprite Icon => _data.Icon;
        public string Name => _data.Name;

        /// <summary>衰减时机：宿主自己的回合边界递减一层。</summary>
        public EPerkDecayTiming DecayTiming => _data.DecayTiming;

        public void AddLayers(int amount_) => Layers += amount_;

        public abstract string GetDescription();

        #region 深拷贝一份独立实例，层数一并带走，供施加到生物与卡牌克隆使用

        public abstract BattlePerk Clone();

        #endregion

        /// <summary>克隆辅助：把本实例的层数带到克隆体上。</summary>
        protected BattlePerk WithLayers(BattlePerk clone_)
        {
            clone_.Layers = Layers;
            return clone_;
        }
    }
}
