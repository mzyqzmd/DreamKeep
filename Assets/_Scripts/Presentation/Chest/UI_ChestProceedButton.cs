using Cysharp.Threading.Tasks;
using _DreamKeep.EngineUtility;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>宝箱段「继续」按钮：场景初始隐藏，领取后由段入口亮出；每次点击收段开地图，
    /// 等待任务只放行一次。点击上报能力继承自 UI_Button。</summary>
    public sealed class UI_ChestProceedButton : UI_Button
    {
        private SegmentSceneManager _segmentManager;
        private UniTaskCompletionSource _proceedSource;

        [Inject]
        public void Construct(SegmentSceneManager segmentManager_) => _segmentManager = segmentManager_;

        private void Awake() => Clicked += OnProceedClicked;

        #region 对外：亮出与等待首次点击

        /// <summary>领取后亮出按钮（场景初始为隐藏）。</summary>
        public void Show() => gameObject.SetActive(true);

        /// <summary>等待玩家点击继续按钮，放行后收束段入口流程。</summary>
        public UniTask WaitProceedAsync()
        {
            _proceedSource = new UniTaskCompletionSource();
            return _proceedSource.Task;
        }

        #endregion

        private void OnProceedClicked()
        {
            // 放行只做一次（收束段入口流程）；收段开地图每次点击都执行，按钮保持可用
            _proceedSource?.TrySetResult();
            _proceedSource = null;
            _segmentManager.CompleteSegment();
        }
    }
}
