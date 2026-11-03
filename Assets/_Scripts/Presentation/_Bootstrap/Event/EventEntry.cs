using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>事件段入口：本段"从哪里开始"的唯一落点——由会话走完两页流程，点「继续」后清空选项组并收段开地图。</summary>
    public sealed class EventEntry : IStartable
    {
        private readonly EventSession _session;
        private readonly UI_EventOptionGroup _options;
        private readonly SegmentSceneManager _segmentManager;

        public EventEntry(EventSession session_, UI_EventOptionGroup options_, SegmentSceneManager segmentManager_)
        {
            _session = session_;
            _options = options_;
            _segmentManager = segmentManager_;
        }

        public void Start() => PlayAsync().Forget();

        // 「继续」不销毁：每次点击都收段开地图（CompleteSegment/OpenMap 幂等），
        // 玩家关掉地图后可再点一次重开；段被切走时整场景卸载，循环随之终结
        private async UniTask PlayAsync()
        {
            await _session.PlayAsync();
            while (true)
            {
                _options.Unlock();
                await _options.WaitChoiceAsync();
                _segmentManager.CompleteSegment();
            }
        }
    }
}
