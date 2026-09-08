namespace _DreamKeep.Gameplay
{
    /// <summary>流程级钩子接口：Domain 只认识此接口，不认识「遗物」这个概念。</summary>
    public interface IBattleHook
    {
        void OnHook(BattleState battleState_);
    }
}
