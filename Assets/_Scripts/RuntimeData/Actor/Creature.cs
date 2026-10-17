using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>生物单元：当前生命、最大生命、格挡与状态层，玩家与敌人各持一份；
    /// 伤害、格挡与状态结算一律只作用于它，不区分阵营。</summary>
    public class Creature
    {
        private readonly List<BattlePerk> _powers = new();

        public int CurrentHp { get; set; }
        public int MaxHp { get; }
        public int Block { get; set; }

        public bool IsDead => CurrentHp <= 0;
        public IReadOnlyList<BattlePerk> Powers => _powers;

        public Creature(int maxHp_)
        {
            MaxHp = maxHp_;
            CurrentHp = maxHp_;
        }

        #region 施加状态：同类型累加层数，未挂载过则克隆一份挂载；返回挂载实例供动作侧发事件
        public BattlePerk ApplyPerk(BattlePerk perk_)
        {
            for (int i = 0; i < _powers.Count; i++)
            {
                if (_powers[i].GetType() != perk_.GetType()) continue;
                _powers[i].AddLayers(perk_.Layers);
                return _powers[i];
            }

            BattlePerk mounted = perk_.Clone();
            _powers.Add(mounted);
            return mounted;
        }
        #endregion

        #region 查询指定状态类型的层数，未挂载时返回 0
        public int GetPerkLayers<T>() where T : BattlePerk
        {
            for (int i = 0; i < _powers.Count; i++)
                if (_powers[i] is T typed)
                    return typed.Layers;
            return 0;
        }
        #endregion

        #region 查询指定状态类型的第一份实例，未挂载时返回 null
        public T GetPerk<T>() where T : BattlePerk
        {
            for (int i = 0; i < _powers.Count; i++)
                if (_powers[i] is T typed)
                    return typed;
            return null;
        }
        #endregion

        #region 回合边界：结算指定时机的状态层数（递减一层），归零项移除并逐个带回，供动作侧发移除事件

        public List<BattlePerk> SettlePerks(EPerkSettleTiming timing_)
        {
            List<BattlePerk> removed = new();
            for (int i = _powers.Count - 1; i >= 0; i--)
            {
                if (_powers[i].SettleTiming != timing_) continue;
                _powers[i].AddLayers(-1);
                if (_powers[i].Layers > 0) continue;
                removed.Add(_powers[i]);
                _powers.RemoveAt(i);
            }

            return removed;
        }
        #endregion
    }
}
