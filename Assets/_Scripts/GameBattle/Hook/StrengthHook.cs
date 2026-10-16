using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态行为「力量」：攻击方每层使伤害 +1（加性阶段贡献）。</summary>
    public static class StrengthHook
    {
        public static int AdditiveOf(BP_Strength strength_, Creature host_, Creature source_) =>
            host_ == source_ ? strength_.Layers : 0;
    }
}
