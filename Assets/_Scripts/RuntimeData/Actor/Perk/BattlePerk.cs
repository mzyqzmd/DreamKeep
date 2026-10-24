using _DreamKeep.ConfigData;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>战斗状态运行时实例：挂在生物身上、层数可变的独立副本，显示名与结算时机转发自状态定义资产（SO_Perk）；
    /// 数值修正由 GameBattle 的 StatHook 分派表向本类取用（要改值的状态暴露 GetModifier，通道与交手方判断、算术都留在这里）；
    /// 触发行为（产动作）必须 new 动作类，故住在 GameBattle 的 SettleHook.Settle 里。</summary>
    public abstract class BattlePerk
    {
        protected readonly SO_Perk _data;

        protected BattlePerk(SO_Perk data_) => _data = data_;

        /// <summary>当前层数：实例私有状态，施加与衰减都只改它。</summary>
        public int Layers { get; private set; }

        public Sprite Icon => _data.Icon;
        public string Name => _data.Name;

        /// <summary>结算时机：宿主自己的回合边界结算一次。</summary>
        public EPerkSettleTiming SettleTiming => _data.SettleTiming;

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
