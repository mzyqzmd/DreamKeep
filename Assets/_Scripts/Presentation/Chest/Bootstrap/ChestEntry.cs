using _DreamKeep.GameRun;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>宝箱段表现入口：启动域流程，并把宝箱段完成事实转成常驻编排者的收段动作。
    /// 视图只订阅事实与提交意图，不再一路 await 面板，也不自行决定收段。</summary>
    public sealed class ChestEntry : IStartable
    {
        private readonly ChestEventDispatcher _events;
        private readonly ChestLoop _loop;
        private readonly SegmentSceneManager _segmentManager;

        public ChestEntry(ChestEventDispatcher events_, ChestLoop loop_, SegmentSceneManager segmentManager_)
        {
            _events = events_;
            _loop = loop_;
            _segmentManager = segmentManager_;
        }

        public void Start()
        {
            _events.Register<CE_ChestCompleted>(completed_ => _segmentManager.CompleteSegment());
            _loop.Start();
        }
    }
}
