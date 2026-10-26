using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态施加：记录来源、目标与挂载实例（表现层槽位直接绑它，层数与描述随域真值刷新）。
    /// 来源供表现层区分"自己给自己"与"别人给自己"。</summary>
    public struct BE_PerkApplied : IBattleEvent
    {
        public Creature Source { get; }
        public Creature Target { get; }
        public BattlePerk Perk { get; }

        public BE_PerkApplied(Creature source_, Creature target_, BattlePerk perk_)
        {
            Source = source_;
            Target = target_;
            Perk = perk_;
        }
    }
}
