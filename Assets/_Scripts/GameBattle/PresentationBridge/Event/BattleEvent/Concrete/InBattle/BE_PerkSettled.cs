using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态结算：回合边界结算后仍挂载的幸存实例，层数已递减，供表现层刷新对应槽位的层数显示。</summary>
    public struct BE_PerkSettled : IBattleEvent
    {
        public Creature Target { get; }
        public BattlePerk Perk { get; }

        public BE_PerkSettled(Creature target_, BattlePerk perk_)
        {
            Target = target_;
            Perk = perk_;
        }
    }
}
