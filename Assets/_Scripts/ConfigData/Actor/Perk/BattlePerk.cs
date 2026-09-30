using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>战斗状态：既是配置资产里被 [SerializeReference] 直挂的状态模板，也是运行时挂在生物身上的状态实例。
    /// 模板层数恒为 0（Layers 是自动属性，Unity 不序列化），施加时由 Clone() 产出独立实例后再叠加层数。</summary>
    [Serializable]
    public abstract class BattlePerk
    {
        [SerializeField] private Sprite _icon;

        /// <summary>当前层数：实例私有状态，配置模板上恒为 0。</summary>
        public int Layers { get; private set; }

        /// <summary>衰减时机：宿主自己的回合边界递减一层，默认回合结束；永久状态覆写为 None。</summary>
        public abstract EPerkDecayTiming DecayTiming { get; }

        public abstract string Name { get; }
        public Sprite Icon => _icon;

        public void AddLayers(int amount_) => Layers += amount_;

        public abstract string GetDescription();

        #region 深拷贝一份独立实例，层数一并带走，供施加到生物与卡牌克隆使用

        public abstract BattlePerk Clone();

        #endregion

        /// <summary>克隆辅助：把本实例的层数与图标带到克隆体上。</summary>
        protected BattlePerk WithLayers(BattlePerk clone_)
        {
            clone_.Layers = Layers;
            clone_._icon = _icon;
            return clone_;
        }
    }
}