using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>生物单元：当前生命、最大生命、格挡与状态层，玩家与敌人各持一份；
    /// 伤害、格挡与状态结算一律只作用于它，不区分阵营。</summary>
    public class Creature
    {
        private readonly List<BattlePerk> _perks = new();

        public string ActorName { get; }
        private int _currentHp;

        /// <summary>当前生命：set 内抛 HpChanged，供整局 HUD（顶栏）订阅刷新；
        /// 敌我共用，敌人变更暂无订阅者亦不影响。</summary>
        public int CurrentHp
        {
            get => _currentHp;
            set
            {
                if (_currentHp == value) return;
                _currentHp = value;
                HpChanged?.Invoke(_currentHp);
            }
        }

        /// <summary>生命变更通知。</summary>
        public event Action<int> HpChanged;
        public int MaxHp { get; }
        public int Block { get; set; }

        public bool IsDead => CurrentHp <= 0;
        public IReadOnlyList<BattlePerk> Perks => _perks;

        public Creature(string actorName_, int maxHp_)
        {
            ActorName = actorName_;
            MaxHp = maxHp_;
            CurrentHp = maxHp_;
        }

        #region 施加状态：同类型累加层数，未挂载过则克隆一份挂载；返回挂载实例供动作侧发事件

        public BattlePerk ApplyPerk(BattlePerk perk_)
        {
            for (int i = 0; i < _perks.Count; i++)
            {
                if (_perks[i].GetType() != perk_.GetType()) continue;
                _perks[i].AddLayers(perk_.Layers);
                return _perks[i];
            }

            BattlePerk mounted = perk_.Clone();
            _perks.Add(mounted);
            return mounted;
        }

        #endregion

        #region 回合边界：结算指定时机的状态层数（递减一层），归零项移除并逐个带回，供动作侧发移除事件

        public List<BattlePerk> SettlePerks(EPerkSettleTiming timing_)
        {
            List<BattlePerk> removed = new();
            for (int i = _perks.Count - 1; i >= 0; i--)
            {
                if (_perks[i].SettleTiming != timing_) continue;
                _perks[i].AddLayers(-1);
                if (_perks[i].Layers > 0) continue;
                removed.Add(_perks[i]);
                _perks.RemoveAt(i);
            }

            return removed;
        }

        #endregion
    }
}