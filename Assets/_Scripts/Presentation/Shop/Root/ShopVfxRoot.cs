using Cysharp.Threading.Tasks;
using VContainer;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>商店段特效根：订阅本段删牌结算完成事实，先播对应音效，再播横向均布演出；
    /// 返回等演出播完的任务，商店流程会停在演出结束前。</summary>
    public sealed class ShopVfxRoot : CardOperationVfxRoot
    {
        private VfxFactory _vfxFactory;

        [Inject]
        public void Construct(ShopEventDispatcher events_, VfxFactory vfxFactory_, AudioRoot audio_)
        {
            _vfxFactory = vfxFactory_;
            events_.Register<SE_CardsOperationFinished>(finished_ =>
            {
                if (audio_) audio_.Play(finished_.Upgraded ? audio_.Cues.Forge : audio_.Cues.RemoveCard);
                return PlayAsync(_vfxFactory, finished_.Cards, finished_.Upgraded);
            });
        }
    }
}
