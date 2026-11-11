using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>休息段特效根：营火烟雾由段入口按事实播放；选牌结算完成事实在此播锻造/删牌演出，
    /// 返回等演出播完的任务，流程会停在演出结束前。落点取锚点矩形中心（与 pivot 无关）。</summary>
    public sealed class RestVfxRoot : CardOperationVfxRoot
    {
        [Header("营火烟雾")] [Tooltip("落点父级：接营火高亮")] [SerializeField]
        private Transform _playVFXParent;

        [Header("倍率")] [SerializeField] private float _restSmokeScale = 1f;

        private VfxFactory _vfxFactory;

        [Inject]
        public void Construct(RestEventDispatcher events_, VfxFactory vfxFactory_, AudioRoot audio_)
        {
            _vfxFactory = vfxFactory_;
            events_.Register<REE_CardsOperationFinished>(finished_ =>
            {
                if (audio_) audio_.Play(finished_.Upgraded ? audio_.Cues.Forge : audio_.Cues.RemoveCard);
                return PlayAsync(_vfxFactory, finished_.Cards, finished_.Upgraded);
            });
        }

        #region 对外播放：营火烟雾播完才返回，供段入口按序回血/收段

        public UniTask PlayRestSmokeAsync()
        {
            if (!_vfxFactory) return UniTask.CompletedTask;
            UniTaskCompletionSource done = new();
            _vfxFactory.PlayRestSmoke(_playVFXParent,
                UiGeometry.AnchorPoint((RectTransform)_playVFXParent, EUiAnchor.MiddleCenter), _restSmokeScale,
                () => done.TrySetResult());
            return done.Task;
        }

        #endregion
    }
}
