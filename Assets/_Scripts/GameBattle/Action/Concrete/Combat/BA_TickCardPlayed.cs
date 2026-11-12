using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>出牌链尾广播：让场上存活敌人的状态各自响应本次出牌，只有层数真变化的状态才发施加事件刷新槽位；
    /// 目标由战场敌人表决定，故 Targets 为空表（同 BA_DamageRandomEnemy 先例）。</summary>
    public sealed class BA_TickCardPlayed : BattleAction
    {
        public BA_TickCardPlayed(Creature source_) : base(source_, new List<Creature>()) { }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            IReadOnlyList<Enemy> enemies = battleContext_.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                Creature target = enemies[i].Creature;
                if (target.IsDead) continue;

                IReadOnlyList<BattlePerk> perks = target.Perks;
                for (int j = 0; j < perks.Count; j++)
                {
                    BattlePerk perk = perks[j];
                    int before = perk.Layers;
                    perk.OnCardPlayed();
                    if (perk.Layers != before) events.Add(new BE_PerkApplied(Source, target, perk));
                }
            }

            return events;
        }
    }
}
