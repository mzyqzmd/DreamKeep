using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态行为「虚弱」：攻击方的伤害减为 ×(1−ratio)（乘性阶段贡献）。</summary>
    public static class WeakHook
    {
        public static float MultiplierOf(BP_Weak weak_, Creature host_, Creature source_) =>
            host_ == source_ ? 1f - weak_.Ratio : 1f;
    }
}
