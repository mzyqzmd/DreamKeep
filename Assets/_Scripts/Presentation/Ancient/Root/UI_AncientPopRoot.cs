using UnityEngine;
using VContainer;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>先古之民段弹层宿主：订阅名号就位事实，经通用弹层工厂 PopFactory 播报横幅；本段只播一次。</summary>
    public sealed class UI_AncientPopRoot : MonoBehaviour
    {
        [Tooltip("横幅挂载父级：一般接本段画布的弹层层")]
        [SerializeField]
        private RectTransform _bannerParent;

        private PopFactory _popFactory;
        private bool _played;

        [Inject]
        public void Construct(PopFactory popFactory_, AncientEventDispatcher events_)
        {
            _popFactory = popFactory_;
            events_.Register<AE_NameplateBound>(bound_ => PlayBanner(bound_.AncientName, bound_.PositionName));
        }

        #region 名号横幅：本段只播一次

        /// <summary>播报名号横幅：主位显名字、副位显职位；重复调用静默。</summary>
        public void PlayBanner(string ancientName_, string positionName_)
        {
            if (_played) return;
            _played = true;
            _popFactory.PopBanner(_bannerParent, ancientName_, positionName_);
        }

        #endregion
    }
}
