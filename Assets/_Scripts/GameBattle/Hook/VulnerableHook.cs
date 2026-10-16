using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态行为「易伤」：受击方受到的伤害 ×(1+ratio)（乘性阶段贡献）。</summary>
    public static class VulnerableHook
    {
        public static float MultiplierOf(BP_Vulnerable vulnerable_, Creature host_, Creature target_) =>
            host_ == target_ ? 1f + vulnerable_.Ratio : 1f;
    }
}
