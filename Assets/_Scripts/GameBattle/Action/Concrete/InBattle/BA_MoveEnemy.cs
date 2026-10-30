using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>位移动作：把目标在敌人槽位间按格数推动，逐格推进、首次受阻即停。
    /// 正数推远（远离玩家）、负数拉近；撞到占位的敌人时撞晕对方（对方本回合意图被替换为击晕），
    /// 推到槽位尽头则位移不再前进（且不击晕任何人）。</summary>
    public sealed class BA_MoveEnemy : BattleAction
    {
        private readonly Enemy _enemy;
        private readonly int _offset;

        public BA_MoveEnemy(Enemy enemy_, int offset_)
            : base(enemy_.Creature, new List<Creature> { enemy_.Creature })
        {
            _enemy = enemy_;
            _offset = offset_;
        }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            int step = Math.Sign(_offset);
            int remaining = Math.Abs(_offset);

            while (remaining-- > 0)
            {
                int destination = _enemy.Slot + step;
                if (destination < BattleSlots.FrontEnemy || destination > BattleSlots.RearEnemy) break;

                Enemy blocker = OccupantOf(battleContext_, destination);
                if (blocker != null)
                {
                    // 撞人：撞晕挡路者，本次位移到此为止
                    blocker.ApplyStun();
                    events.Add(new BE_EnemyIntentChanged(blocker, blocker.PreviewIntent));
                    break;
                }

                int fromSlot = _enemy.Slot;
                _enemy.SetSlot(destination);
                events.Add(new BE_EnemyMoved(_enemy, fromSlot, destination));
            }

            return events;
        }

        // 找目的地上的存活敌人：槽位互斥，最多一只
        private static Enemy OccupantOf(BattleContext battleContext_, int slot_)
        {
            IReadOnlyList<Enemy> enemies = battleContext_.Enemies;
            for (int i = 0; i < enemies.Count; i++)
                if (!enemies[i].Creature.IsDead && enemies[i].Slot == slot_) return enemies[i];

            return null;
        }
    }
}
