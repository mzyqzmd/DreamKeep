using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>弹层宿主：挂在场景 Canvas_Pop/PopTextRoot 上，持有本战斗的弹层文本——
    /// 说明气泡（悬停类：内容与世界锚点由调用方给，此处只管单条实例复用与定位）与回合横幅（公告类：自订阅 BE_TurnStarted 转发展示）；
    /// 运行期不认识具体调用方（仅装配期注入事件门面）。</summary>
    public class UI_PopTextRoot : MonoBehaviour
    {
        [SerializeField] private UI_DescriptionPopTip _descriptionPopTipPrefab;
        [SerializeField] private UI_TurnCountPopTip _turnCountPopTipPrefab;

        private UI_DescriptionPopTip _descriptionPopTip;
        private UI_TurnCountPopTip _turnCountPopTip;
        private object _requester;

        #region 对外展示：单条说明气泡在给定世界锚点上方弹出，按发起者收起

        /// <summary>弹出单条说明气泡：底沿贴住 worldAnchor_（通常传槽位顶沿世界坐标），标题为空自动收起标题行。</summary>
        public void ShowTip(object requester_, string name_, string description_, Vector3 worldAnchor_)
        {
            if (_descriptionPopTip == null)
            {
                _descriptionPopTip = Instantiate(_descriptionPopTipPrefab, transform);
                ((RectTransform)_descriptionPopTip.transform).pivot = new Vector2(0.5f, 0f);
            }

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

        #region 接线：订阅回合开始事件转发横幅展示

        [Inject]
        public void Construct(BattleEventDispatcher eventDispatcher_) =>
            eventDispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);

        #endregion

        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            ShowTurnBanner(started_.IsPlayerTurn, started_.TurnCount);
            return UniTask.CompletedTask;
        }

        #region 回合横幅：懒生成单实例并转发展示

        // 横幅自带"收拢→停留→渐隐"时间线且不由域侧 await，与抽牌等回合演出同屏并行
        private void ShowTurnBanner(bool isPlayerTurn_, int turnCount_)
        {
            if (_turnCountPopTip == null) _turnCountPopTip = Instantiate(_turnCountPopTipPrefab, transform);
            _turnCountPopTip.Show(isPlayerTurn_, turnCount_);
        }

        #endregion
    }
}
