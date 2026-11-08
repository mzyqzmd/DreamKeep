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

        /// <summary>当前生命：set 内抛 HpChanged，供整局 HUD（顶栏）订阅刷新；上升时另抛 Healed；
        /// 敌我共用，敌人变更暂无订阅者亦不影响。</summary>
        public int CurrentHp
        {
            get => _currentHp;
            set
            {
                if (_currentHp == value) return;
                int delta = value - _currentHp;
                _currentHp = value;
                HpChanged?.Invoke(_currentHp);
                if (delta >= 0) Healed?.Invoke(delta); //只要回血就播vfx,恢复0点也是回血
                else HpLost?.Invoke(-delta);
            }
        }

        /// <summary>生命变更通知。</summary>
        public event Action<int> HpChanged;

        /// <summary>回血通知：CurrentHp 上升时抛回复量，覆盖战斗内直写与局外结算全部路径；
        /// 是否播放由表现层按场景判定。敌人回血同样抛，暂无订阅者亦不影响。</summary>
        public event Action<int> Healed;

        /// <summary>失去生命通知：CurrentHp 下降时抛实际损失量（保底留 1 之后的真实差值），与 <see cref="Healed"/> 对称；
        /// 是否播放由表现层按场景判定。快照回滚走 RestoreHpTo 不经过 setter，不会触发。</summary>
        public event Action<int> HpLost;

        /// <summary>最大生命：构造期取配置初值，局内可经 <see cref="RaiseMaxHp"/> 增长（战斗内恒定）。</summary>
        public int MaxHp { get; private set; }

        public int Block { get; set; }

        public bool IsDead => CurrentHp <= 0;
        public IReadOnlyList<BattlePerk> Perks => _perks;

        public Creature(string actorName_, int maxHp_)
        {
            ActorName = actorName_;
            MaxHp = maxHp_;
            CurrentHp = maxHp_;
        }

        #region 快照回滚：写回已知合法值并刷新 HUD，但不算回血（不抛 Healed）

        /// <summary>快照回滚专用：撤销自选面板的扣血代价时写回旧值——HpChanged 照抛让 HUD 跟上，
        /// 但不抛 Healed（玩家并未回血，不该播回血特效）。</summary>
        public void RestoreHpTo(int value_)
        {
            if (_currentHp == value_) return;
            _currentHp = value_;
            HpChanged?.Invoke(_currentHp);
        }

        /// <summary>读档重建：同时写回上限与当前生命，只抛 HpChanged 刷新 HUD，不抛 Healed（不算回血）。</summary>
        public void RestoreVitals(int currentHp_, int maxHp_)
        {
            MaxHp = maxHp_;
            if (_currentHp == currentHp_) return;
            _currentHp = currentHp_;
            HpChanged?.Invoke(_currentHp);
        }

        #endregion

        #region 提升最大生命：烹饪这类局内效果用，上限与当前生命同步加，非正数直接忽略

        /// <summary>提高最大生命上限，当前生命同步加同量（等价于"上限+N 并回复 N 点"）；顶栏经 HpChanged 自动刷新。</summary>
        public void RaiseMaxHp(int amount_)
        {
            if (amount_ <= 0) return;
            MaxHp += amount_;
            CurrentHp += amount_; // 先抬上限再回血，CurrentHp 恒不超过 MaxHp
        }

        #endregion

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

        /// <summary>移除单个状态：按运行时类型在列表中匹配，移除并返回该实例供动作侧发事件，找不到返回 null。</summary>
        public BattlePerk RemovePerk(BattlePerk perk_)
        {
            for (int i = 0; i < _perks.Count; i++)
            {
                if (_perks[i].GetType() != perk_.GetType()) continue;
                BattlePerk removed = _perks[i];
                _perks.RemoveAt(i);
                return removed;
            }

            return null;
        }

        /// <summary>清空身上全部状态：战斗收尾调用，perk 只在本场有效，不带进下一场。</summary>
        public void ClearPerks() => _perks.Clear();

        #endregion

        #region 回合边界：按状态自身规则结算指定时机的层数，要求移除的项逐个带回，供动作侧发移除事件

        public List<BattlePerk> SettlePerks(EPerkSettleTiming timing_)
        {
            List<BattlePerk> removed = new();
            for (int i = _perks.Count - 1; i >= 0; i--)
            {
                if (_perks[i].SettleTiming != timing_) continue;
                if (!_perks[i].Settle()) continue;
                removed.Add(_perks[i]);
                _perks.RemoveAt(i);
            }

            return removed;
        }

        #endregion
    }
}