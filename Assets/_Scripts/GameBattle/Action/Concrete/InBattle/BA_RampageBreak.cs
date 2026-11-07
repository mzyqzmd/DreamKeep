using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>横冲直撞打断动作：移除横冲直撞与力量（各发移除事件，玩家给的其它状态不动）、
    /// 整表切入阶段二、施加击晕并广播；由 HpDropHook 在伤害落地的同一次结算里嵌套执行（只合事件不进队列）。</summary>
    public sealed class BA_RampageBreak : BattleAction
    {
        public BA_RampageBreak(Creature source_) : base(source_, new List<Creature> { source_ }) { }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();

            // 幂等守卫：横冲直撞已不在身上（重复触发/已被结算清走）就不重复打断
            if (FindPerk<BP_Rampage>() is not BP_Rampage rampage) return events;

            BattlePerk removedRampage = Source.RemovePerk(rampage);
            if (removedRampage != null) events.Add(new BE_PerkRemoved(Source, removedRampage));

            // 力量无论正负一并移除：后续再施加走新挂载分支，等价于重置为 0
            if (FindPerk<BP_Strength>() is BP_Strength strength)
            {
                BattlePerk removedStrength = Source.RemovePerk(strength);
                if (removedStrength != null) events.Add(new BE_PerkRemoved(Source, removedStrength));
            }

            // 整表切阶段二并击晕：意图预告立即变击晕，敌人回合消费后从阶段二第一条开始
            Enemy enemy = FindEnemy(battleContext_);
            if (enemy == null) return events;

            enemy.SwitchToPhaseTwo();
            enemy.ApplyStun();
            events.Add(new BE_EnemyStunned(enemy));
            events.Add(new BE_EnemyIntentChanged(enemy, enemy.PreviewIntent));

            return events;
        }

        // 按运行时类型在宿主身上找挂载实例：同类型状态唯一
        private T FindPerk<T>() where T : BattlePerk
        {
            IReadOnlyList<BattlePerk> perks = Source.Perks;
            for (int i = 0; i < perks.Count; i++)
                if (perks[i] is T match) return match;

            return null;
        }

        // 由 Creature 反查所属敌人：横冲直撞只存在于敌人身上，找不到即数据异常，放弃后续演出
        private Enemy FindEnemy(BattleContext battleContext_)
        {
            IReadOnlyList<Enemy> enemies = battleContext_.Enemies;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i].Creature == Source) return enemies[i];

            return null;
        }
    }
}
