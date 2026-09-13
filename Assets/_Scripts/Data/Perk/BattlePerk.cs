using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>战斗状态：既是配置资产里被 [SerializeReference] 直挂的状态模板，也是运行时挂在生物身上的状态实例。
    /// 模板层数恒为 0（Layers 是自动属性，Unity 不序列化），施加时由 Clone() 产出独立实例后再叠加层数。</summary>
    [Serializable]
    public abstract class BattlePerk
    {
        [SerializeField] private Sprite _icon;

        /// <summary>当前层数：实例私有状态，配置模板上恒为 0。</summary>
        public int Layers { get; private set; }

        /// <summary>回合结束是否自动衰减一层；力量等永久状态覆写为 false。</summary>
        public virtual bool DecaysEachTurn => true;

        public abstract string Name { get; }
        public Sprite Icon => _icon;

        #region 叠加层数
        public void AddLayers(int amount_) => Layers += amount_;
        #endregion

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
