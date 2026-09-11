using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>战斗单位基类：血量、格挡与状态层，敌我共用；能量为玩家专属故下沉至 Player。</summary>
    public class Actor
    {
        private readonly List<BattlePerk> _perks = new();

        public int Health { get; set; }
        public int Block { get; set; }
        public bool IsDead => Health <= 0;
        public IReadOnlyList<BattlePerk> Perks => _perks;

        public Actor(int health_, int block_)
        {
            Health = health_;
            Block = block_;
        }

        #region 施加状态：同类型累加层数，未挂载过则克隆挂载；返回施加后的总层数
        public int ApplyPerk(BattlePerk perk_)
        {
            Type perkType = perk_.GetType();
            foreach (BattlePerk perk in _perks)
            {
                if (perk.GetType() == perkType)
                {
                    perk.AddLayers(perk_.Layers);
                    return perk.Layers;
                }
            }
            BattlePerk cloned = perk_.Clone();
            _perks.Add(cloned);
            return cloned.Layers;
        }
        #endregion

        #region 查询指定状态类型的层数，未挂载时返回 0
        public int GetPerkLayers<T>() where T : BattlePerk
        {
            foreach (BattlePerk perk in _perks)
                if (perk is T typed)
                    return typed.Layers;
            return 0;
        }
        #endregion

        #region 回合结束：会衰减的状态层数递减，归零项移除
        public void DecayPerks()
        {
            for (int i = _perks.Count - 1; i >= 0; i--)
            {
                if (!_perks[i].DecaysEachTurn) continue;
                _perks[i].AddLayers(-1);
                if (_perks[i].Layers <= 0)
                    _perks.RemoveAt(i);
            }
        }
        #endregion
    }
}
