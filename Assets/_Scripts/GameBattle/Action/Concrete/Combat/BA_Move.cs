using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人移动动作：向玩家方向前进一个槽位；前槽有盟友则与它互换槽位一起轮换，站最前敌人槽位则回绕到最靠后空槽。</summary>
    public sealed class BA_Move : BattleAction
    {
        private readonly Enemy _enemy;

        public BA_Move(Enemy enemy_) : base(enemy_.Creature, new List<Creature> { enemy_.Creature }) => _enemy = enemy_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            int fromSlot = _enemy.Slot;

            // 前槽有盟友：互换而非白过，两只都完成一次轮换；按槽位从左到右执行，盟友本回合已行动或已跳过，不会被重复驱动
            Enemy frontAlly = fromSlot > BattleSlots.FrontEnemy ? FrontAllyOf(battleContext_) : null;
            if (frontAlly != null)
            {
                int allySlot = frontAlly.Slot;
                _enemy.SetSlot(allySlot);
                frontAlly.SetSlot(fromSlot);
                events.Add(new BE_EnemyMoved(_enemy, fromSlot, allySlot));
                events.Add(new BE_EnemyMoved(frontAlly, allySlot, fromSlot));
                return events;
            }

            int toSlot = ResolveTargetSlot(battleContext_);
            if (toSlot == fromSlot) return events;

            _enemy.SetSlot(toSlot);
            events.Add(new BE_EnemyMoved(_enemy, fromSlot, toSlot));
            return events;
        }

        /// <summary>算目标槽位：站最前敌人槽位回绕到最靠后空槽，否则前进一格。</summary>
        private int ResolveTargetSlot(BattleContext battleContext_)
        {
            if (_enemy.Slot == BattleSlots.FrontEnemy)
            {
                int rearSlot = battleContext_.GetRearmostEmptySlot(_enemy);
                return rearSlot < 0 ? _enemy.Slot : rearSlot;
            }
            return _enemy.Slot - 1;
        }

        /// <summary>取前槽位上的存活盟友：槽位互斥，最多一只；玩家不占敌人槽位。</summary>
        private Enemy FrontAllyOf(BattleContext battleContext_)
        {
            int frontSlot = _enemy.Slot - 1;
            IReadOnlyList<Enemy> enemies = battleContext_.Enemies;
            for (int i = 0; i < enemies.Count; i++)
                if (!enemies[i].Creature.IsDead && enemies[i].Slot == frontSlot) return enemies[i];

            return null;
        }
    }
}
