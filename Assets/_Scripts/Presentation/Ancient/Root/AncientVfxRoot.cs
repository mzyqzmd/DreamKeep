using Cysharp.Threading.Tasks;
using VContainer;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>先古段特效根：订阅本段删牌/升级结算完成事实，先播对应音效，再播横向均布演出；
    /// 返回等演出播完的任务，先古流程会停在演出结束前。</summary>
    public sealed class AncientVfxRoot : CardOperationVfxRoot
    {
        private VfxFactory _vfxFactory;

        [Inject]
        public void Construct(AncientEventDispatcher events_, VfxFactory vfxFactory_, AudioRoot audio_)
        {
            _vfxFactory = vfxFactory_;
            events_.Register<AE_CardsOperationFinished>(finished_ =>
            {
                if (audio_) audio_.Play(finished_.Upgraded ? audio_.Cues.Forge : audio_.Cues.RemoveCard);
                return PlayAsync(_vfxFactory, finished_.Cards, finished_.Upgraded);
            });
        }
    }
}
