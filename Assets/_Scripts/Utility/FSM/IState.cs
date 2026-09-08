namespace _DreamKeep.Utility
{
    /// <summary>
    /// 状态接口，定义状态的生命周期回调。
    /// </summary>
    public interface IState
    {
        void Enter();
        void Update();
        void Exit();
    }
}
