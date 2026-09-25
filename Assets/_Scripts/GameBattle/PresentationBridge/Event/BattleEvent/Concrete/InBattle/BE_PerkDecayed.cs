using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态递减：回合边界层数减一后仍挂载的幸存实例，供表现层刷新对应槽位的层数显示。</summary>
    public struct BE_PerkDecayed : IBattleEvent
    {
        public Creature Target { get; }
        public BattlePerk Perk { get; }

        public BE_PerkDecayed(Creature target_, BattlePerk perk_)
        {
            Target = target_;
            Perk = perk_;
        }
    }
}
