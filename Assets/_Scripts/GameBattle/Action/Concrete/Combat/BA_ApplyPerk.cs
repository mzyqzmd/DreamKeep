using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>施加状态动作：为目标挂载状态层数，同类型累加。</summary>
    public sealed class BA_ApplyPerk : BattleAction
    {
        /// <summary>状态实例，层数由实例携带。</summary>
        public BattlePerk Perk { get; }

        public BA_ApplyPerk(Creature source_, List<Creature> targets_, BattlePerk perk_) : base(source_, targets_) =>
            Perk = perk_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            Player player = battleContext_.Player;
            int maxEnergyBefore = player.MaxEnergy;
            foreach (Creature target in Targets)
            {
                if (target.IsDead) continue;
                BattlePerk mounted = target.ApplyPerk(Perk);
                if (mounted is BP_Shrink shrink) shrink.SetSource(Source);
                events.Add(new BE_PerkApplied(Source, target, mounted));
                // 挂载完成后询问施加方：凶恶在给予易伤时抽牌
                PerkApplyHook.Check(battleContext_, Source, target, mounted, events);
            }

            // 上限由身上状态汇总得出：施加后变化则补发能量事件，供能量文本当场刷新
            if (player.MaxEnergy != maxEnergyBefore)
                events.Add(new BE_EnergyChanged(player.CombatState.CurrentEnergy, player.MaxEnergy));

            return events;
        }
    }
}
