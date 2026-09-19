using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>消耗堆视图：提供归宿终点锚点，带消耗与遗忘标记的牌飞向该点并缩到 0 后回收；消耗演出播完后把已消耗张数写到计数文本。
    /// 遗忘的牌只借道此处回收、域侧不入本堆（永久移出卡组），故不计数。</summary>
    public class UI_ExhaustPile : MonoBehaviour
    {
        [Tooltip("计数文本：显示消耗堆已消耗张数")] [SerializeField]
        private TextMeshProUGUI _countText;

        private CardPile _exhaustPile;

        public Vector3 Anchor => transform.position;

        #region 接线：消耗演出播完后刷新已消耗张数

        [Inject]
        public void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_)
        {
            _exhaustPile = battleContext_.Player.CombatState.ExhaustPile;
            _countText.text = _exhaustPile.Count.ToString(); // 进战即显示：开局无消耗
            eventDispatcher_.Register<BE_CardExhausted>(RefreshCountAsync);
        }

        #endregion

        private UniTask RefreshCountAsync(BE_CardExhausted exhausted_)
        {
            _countText.text = _exhaustPile.Count.ToString();
            return UniTask.CompletedTask;
        }
    }
}