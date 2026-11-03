using _DreamKeep.EngineUtility;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>商店「继续」按钮：首次掀开地毯后亮出（场景初始是关的），
    /// 每次点击都关会话开地图并保留本段，玩家关掉地图后可再点一次重开。</summary>
    public sealed class UI_ShopProceedItem : UI_Button
    {
        private SegmentSceneManager _segmentManager;

        [Inject]
        public void Construct(SegmentSceneManager segmentManager_) => _segmentManager = segmentManager_;

        private void Awake() => Clicked += OnClicked;

        #region 对外：亮出

        /// <summary>亮出按钮：段入口在玩家首次掀开地毯时调用。</summary>
        public void Show() => gameObject.SetActive(true);

        #endregion

        private void OnClicked() => _segmentManager.CompleteSegment();
    }
}
