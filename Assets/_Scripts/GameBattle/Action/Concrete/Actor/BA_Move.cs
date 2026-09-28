using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人移动动作：向玩家方向前进一个槽位；前槽被占则白过，站 0 号（前面没有槽位）则回绕到最靠后空槽。</summary>
    public class BA_Move : BattleAction
    {
        private readonly Enemy _enemy;

        public BA_Move(Enemy enemy_) : base(enemy_.Creature, new List<Creature> { enemy_.Creature }) => _enemy = enemy_;

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            int toSlot = ResolveTargetSlot(battleContext_);
            if (toSlot == _enemy.Slot) return events;

            int fromSlot = _enemy.Slot;
            _enemy.SetSlot(toSlot);
            events.Add(new BE_EnemyMoved(_enemy, fromSlot, toSlot));
            return events;
        }

        /// <summary>算目标槽位：站 0 号回绕到最靠后空槽，否则前槽为空前进一格、被挡原地不动。</summary>
        private int ResolveTargetSlot(BattleContext battleContext_)
        {
            if (_enemy.Slot == 0)
            {
                int rearSlot = battleContext_.GetRearmostEmptySlot(_enemy);
                return rearSlot < 0 ? _enemy.Slot : rearSlot;
            }
            if (battleContext_.IsSlotOccupied(_enemy.Slot - 1, _enemy)) return _enemy.Slot;
            return _enemy.Slot - 1;
        }
    }
}
