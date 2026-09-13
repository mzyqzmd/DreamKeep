using _DreamKeep.Data;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>状态施加：记录目标、状态与施加后的总层数。</summary>
    public struct BE_PerkApplied : IBattleEvent
    {
        public Creature Target { get; }
        public BattlePerk Perk { get; }
        public int Layers { get; }

        public BE_PerkApplied(Creature target_, BattlePerk perk_, int layers_)
        {
            Target = target_;
            Perk = perk_;
            Layers = layers_;
        }
    }
}
