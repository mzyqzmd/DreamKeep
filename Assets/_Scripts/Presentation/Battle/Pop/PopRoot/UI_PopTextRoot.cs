using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>弹层宿主：挂在场景 Canvas_Pop/PopTextRoot 上，持有本战斗的弹层文本——
    /// 描述气泡容器（悬停类：条目与锚点由调用方给，容器自己管池化与排布）与回合横幅（公告类：自订阅 BE_TurnStarted 转发展示）；
    /// 运行期不认识容器（仅装配期注入事件门面）。</summary>
    public class UI_PopTextRoot : MonoBehaviour
    {
        [SerializeField] private UI_DescriptionPopTipGroup _descriptionPopTipGroup;
        [SerializeField] private UI_TurnCountPopTip _turnCountPopTipPrefab;

        private UI_TurnCountPopTip _turnCountPopTip;

        /// <summary>描述气泡容器：悬停类提示统一经它弹收（一组条目一次弹出）。</summary>
        public UI_DescriptionPopTipGroup DescriptionTipGroup => _descriptionPopTipGroup;

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
