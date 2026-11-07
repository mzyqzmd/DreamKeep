using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>本次启动的读档恢复上下文：由 RunLifetimeScope 构造一次，段装配与战斗入口据此走奖励结算恢复分支。
    /// 恢复分支消费完即清空，避免之后新开的段被误判成恢复态。</summary>
    public sealed class SaveRestoreContext
    {
        private SaveData _data;

        public SaveRestoreContext(SaveData data_, ESaveStartIntent intent_)
        {
            _data = data_;
            Intent = intent_;
        }

        public SaveData Data => _data;
        public ESaveStartIntent Intent { get; }
        public bool IsAwaitingReward => _data?.SegmentContext == ESaveSegmentContext.AwaitingReward;

        public void Consume() => _data = null;
    }
}
