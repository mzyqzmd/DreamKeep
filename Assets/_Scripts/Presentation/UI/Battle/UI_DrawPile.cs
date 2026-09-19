using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>抽牌堆视图：提供抽牌起点锚点，抽牌入场演出从该点起飞；抽牌演出播完后把剩余张数写到计数文本。
    /// TODO 弃牌堆洗回与抽空的反馈（BE_NoCardCanDraw）未实现。</summary>
    public class UI_DrawPile : MonoBehaviour
    {
        [Tooltip("计数文本：显示抽牌堆剩余张数")] [SerializeField]
        private TextMeshProUGUI _countText;

        private CardPile _drawPile;

        public Vector3 Anchor => transform.position;

        #region 接线：抽牌演出播完后刷新剩余张数

        [Inject]
        public void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_)
        {
            _drawPile = battleContext_.Player.CombatState.DrawPile;
            _countText.text = _drawPile.Count.ToString(); // 进战即显示：此时抽牌堆已是洗好的整副牌
            eventDispatcher_.Register<BE_CardDrawn>(RefreshCountAsync);
        }

        #endregion

        // 抽走一张后重读张数；抽牌堆见底时域侧已把弃牌堆整体洗回，这里读到的也是洗回后的值
        private UniTask RefreshCountAsync(BE_CardDrawn drawn_)
        {
            _countText.text = _drawPile.Count.ToString();
            return UniTask.CompletedTask;
        }
    }
}
