using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>弹层宿主：挂在场景 Canvas_Pop/PopTextRoot 上，持有本战斗的弹层文本——
    /// 说明气泡（悬停类：内容与世界锚点由调用方给，此处只管单条实例复用与定位）与回合横幅（公告类：自订阅 BE_TurnStarted 转发展示）；
    /// 运行期不认识具体调用方（仅装配期注入事件门面）。</summary>
    public sealed class UI_PopTextRoot : MonoBehaviour
    {
        [SerializeField] private UI_DescriptionPopTip _descriptionPopTipPrefab;
        [SerializeField] private UI_TurnCountPopTip _turnCountPopTipPrefab;

        private UI_DescriptionPopTip _descriptionPopTip;
        private UI_TurnCountPopTip _turnCountPopTip;
        private object _requester;

        #region 单条描述气泡说明

        /// <summary>弹出单条说明气泡：底沿贴住 worldAnchor_（通常传槽位顶沿世界坐标），标题为空自动收起标题行。</summary>
        public void ShowTip(object requester_, string name_, string description_, Vector3 worldAnchor_)
        {
            if (_descriptionPopTip == null)
                _descriptionPopTip = Instantiate(_descriptionPopTipPrefab, transform);

            _requester = requester_;
            _descriptionPopTip.SetContent(name_, description_);
            _descriptionPopTip.gameObject.SetActive(true);
            _descriptionPopTip.transform.SetAsLastSibling();
            _descriptionPopTip.transform.position = worldAnchor_;
        }

        /// <summary>收起：只认当前发起者，防鼠标在槽位间移动时迟到的 Exit 收掉刚弹出的新气泡。</summary>
        public void HideTip(object requester_)
        {
            if (_requester != requester_) return;
            _requester = null;
            if (_descriptionPopTip != null) _descriptionPopTip.gameObject.SetActive(false);
        }

        #endregion

        #region 回合横幅弹出

        [Inject]
        public void Construct(BattleEventDispatcher eventDispatcher_) =>
            eventDispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);


        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            if (_turnCountPopTip == null) _turnCountPopTip = Instantiate(_turnCountPopTipPrefab, transform);
            _turnCountPopTip.Show(started_.IsPlayerTurn, started_.TurnCount);
            return UniTask.CompletedTask;
        }

        #endregion
    }
}