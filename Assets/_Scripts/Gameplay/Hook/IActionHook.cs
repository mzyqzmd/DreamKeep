namespace _DreamKeep.Gameplay
{
    /// <summary>动作级钩子非泛型接口：供拦截点注册表统一存放与顺序触发。</summary>
    public interface IActionHook
    {
        void Invoke(GameAction action_);
    }
}
