using _DreamKeep.ConfigData;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>战斗状态运行时实例：挂在生物身上、层数可变的独立副本，显示名与衰减时机转发自状态定义资产（SO_Perk）；
    /// 数值修正住在三个虚方法里，由 GameBattle 的伤害管线遍历调用（新增状态只要覆写即可被认识，不必回改管线）；
    /// 触发行为（产动作）必须 new 动作类，故住在 GameBattle 的触发 resolver 里。</summary>
    public abstract class BattlePerk
    {
        protected readonly SO_Perk _data;

        protected BattlePerk(SO_Perk data_) => _data = data_;

        /// <summary>当前层数：实例私有状态，施加与衰减都只改它。</summary>
        public int Layers { get; private set; }

        public Sprite Icon => _data.Icon;
        public string Name => _data.Name;

        /// <summary>衰减时机：宿主自己的回合边界递减一层。</summary>
        public EPerkDecayTiming DecayTiming => _data.DecayTiming;

        public void AddLayers(int amount_) => Layers += amount_;

        #region 伤害数值修正：宿主与交手双方由伤害管线遍历时传入（状态实例也被卡牌当原型持有，故不持有宿主引用）

        /// <summary>加性贡献：默认 0，即不改变这次伤害。</summary>
        public virtual int ModifyDamageAdditive(Creature host_, Creature source_, Creature target_) => 0;

        /// <summary>乘性贡献：默认 1，即不改变这次伤害。</summary>
        public virtual float ModifyDamageMultiplicative(Creature host_, Creature source_, Creature target_) => 1f;

        /// <summary>上限贡献：管线取所有宿主给出的最小值截断伤害，默认不截断。</summary>
        public virtual int CapDamage(Creature host_, Creature source_, Creature target_) => int.MaxValue;

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
