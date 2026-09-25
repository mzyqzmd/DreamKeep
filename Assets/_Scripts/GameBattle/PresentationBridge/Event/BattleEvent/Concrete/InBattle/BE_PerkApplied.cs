using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态施加：记录目标与挂载实例（表现层槽位直接绑它，层数与描述随域真值刷新）。</summary>
    public struct BE_PerkApplied : IBattleEvent
    {
        public Creature Target { get; }
        public BattlePerk Perk { get; }

        public BE_PerkApplied(Creature target_, BattlePerk perk_)
        {
            Target = target_;
            Perk = perk_;
        }
    }
}
