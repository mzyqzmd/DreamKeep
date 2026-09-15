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

        #region 施加状态：同类型累加层数，未挂载过则克隆一份挂载；返回施加后的总层数
        public int ApplyPerk(BattlePerk perk_)
        {
            for (int i = 0; i < _powers.Count; i++)
            {
                if (_powers[i].GetType() != perk_.GetType()) continue;
                _powers[i].AddLayers(perk_.Layers);
                return _powers[i].Layers;
            }

            BattlePerk mounted = perk_.Clone();
            _powers.Add(mounted);
            return mounted.Layers;
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

        #region 回合结束：会衰减的状态层数递减，归零项移除
        public void DecayPerks()
        {
            for (int i = _powers.Count - 1; i >= 0; i--)
            {
                if (!_powers[i].DecaysEachTurn) continue;
                _powers[i].AddLayers(-1);
                if (_powers[i].Layers <= 0)
                    _powers.RemoveAt(i);
            }
        }
        #endregion
    }
}
