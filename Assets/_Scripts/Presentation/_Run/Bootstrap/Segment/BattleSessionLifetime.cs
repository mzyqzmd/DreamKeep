using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗会话生命周期：持有 BattleSession 与其表现孪生 BattleStage 的句柄，保证成对开关。
    /// 关闭顺序固定——先 session（常驻药水栏借旧表现引用注销瞄准，此时旧中间件还活着）后 stage。</summary>
    public sealed class BattleSessionLifetime
    {
        private readonly BattleSession _session;
        private readonly BattleStage _stage;

        public BattleSessionLifetime(BattleSession session_, BattleStage stage_)
        {
            _session = session_;
            _stage = stage_;
        }

        public void Close()
        {
            if (!_session.IsOpen) return;
            _session.Close();
            _stage.Close();
        }
    }
}
