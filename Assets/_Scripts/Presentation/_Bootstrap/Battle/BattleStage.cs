namespace _DreamKeep.Presentation
{
    /// <summary>战斗段表现句柄：常驻层够到当前战斗段表现锚点（战场单位根与目标裁决中间件）的唯一通道。
    /// 与 BattleSession 成对开关、由段侧装配同一处维护：开段先 stage 后 session（重绑时表现引用已就绪），
    /// 关段先 session 后 stage（药水栏借旧中间件注销瞄准时旧引用仍有效）。段不在场时引用为空，消费方自行判空。</summary>
    public sealed class BattleStage
    {
        public UI_ActorRoot ActorRoot { get; private set; }
        public BattleTargetDirector TargetDirector { get; private set; }

        #region 开关会话：段装配完成时写入，段收尾时清空

        public void Open(UI_ActorRoot actorRoot_, BattleTargetDirector targetDirector_)
        {
            ActorRoot = actorRoot_;
            TargetDirector = targetDirector_;
        }

        public void Close()
        {
            ActorRoot = null;
            TargetDirector = null;
        }

        #endregion
    }
}
