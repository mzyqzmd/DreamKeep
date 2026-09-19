using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>弃牌堆视图：提供归宿终点锚点，离场的牌飞向该点并缩到 0 后回收；弃牌演出播完后把已弃张数写到计数文本。
    /// 抽牌堆见底时域侧会把弃牌堆整体洗回抽牌堆，故抽牌事件同样要刷新。</summary>
    public class UI_DiscardPile : MonoBehaviour
    {
        [Tooltip("计数文本：显示弃牌堆已弃张数")] [SerializeField]
        private TextMeshProUGUI _countText;

        private CardPile _discardPile;

        public Vector3 Anchor => transform.position;

        #region 接线：弃牌与洗回演出播完后刷新已弃张数

        [Inject]
        public void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_)
        {
            _discardPile = battleContext_.Player.CombatState.DiscardPile;
            _countText.text = _discardPile.Count.ToString(); // 进战即显示：开局无弃牌
            eventDispatcher_.Register<BE_CardDiscarded>(RefreshCountAsync);
            eventDispatcher_.Register<BE_CardDrawn>(RefreshCountAsync);
        }

        #endregion

        // 弃牌堆唯一会减少的路径是整体洗回抽牌堆（由抽牌事件触发），两条路径都只需重读张数
        private UniTask RefreshCountAsync(BE_CardDiscarded discarded_)
        {
            _countText.text = _discardPile.Count.ToString();
            return UniTask.CompletedTask;
        }

        private UniTask RefreshCountAsync(BE_CardDrawn drawn_)
        {
            _countText.text = _discardPile.Count.ToString();
            return UniTask.CompletedTask;
        }
    }
}