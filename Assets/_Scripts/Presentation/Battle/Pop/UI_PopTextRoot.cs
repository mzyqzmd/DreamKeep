using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>弹层宿主：挂在场景 Canvas_Pop/PopTextRoot 上，懒生成并持有本战斗的弹层文本——
    /// 描述提示（悬停类：实例经 DescriptionTip 交给调用方驱动内容与定位）与回合横幅（公告类：自订阅 BE_TurnStarted 转发展示）；
    /// 一律首次使用才普通 Instantiate、之后复用，运行期不认识容器（仅装配期注入事件门面）。</summary>
    public class UI_PopTextRoot : MonoBehaviour
    {
        [SerializeField] private UI_DescriptionPopTip _descriptionPopTipPrefab;
        [SerializeField] private UI_TurnCountPopTip _turnCountPopTipPrefab;

        private UI_DescriptionPopTip _descriptionPopTip;
        private UI_TurnCountPopTip _turnCountPopTip;

        /// <summary>共享描述提示：首次访问由宿主生成挂到自己名下（底沿中点对准定位点：高度随内容变化只向上生长）。</summary>
        public UI_DescriptionPopTip DescriptionTip
        {
            get
            {
                if (_descriptionPopTip == null)
                {
                    _descriptionPopTip = Instantiate(_descriptionPopTipPrefab, transform);
                    ((RectTransform)_descriptionPopTip.transform).pivot = new Vector2(0.5f, 0f);
                }

                return _descriptionPopTip;
            }
        }

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
