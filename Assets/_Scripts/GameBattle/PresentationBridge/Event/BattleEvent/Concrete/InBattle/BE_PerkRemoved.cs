using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态移除：回合边界结算归零把状态从生物身上摘除，Perk 是被摘下的挂载实例，供表现层删对应槽位。</summary>
    public struct BE_PerkRemoved : IBattleEvent
    {
        public Creature Target { get; }
        public BattlePerk Perk { get; }

        public BE_PerkRemoved(Creature target_, BattlePerk perk_)
        {
            Target = target_;
            Perk = perk_;
        }
    }
}
