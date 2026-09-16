using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>卡牌拖拽能力：按下抓起抬高、拖动跟手、松手把落点交给宿主判定（打出或回位）；卡牌上配了该组件才有拖拽效果，未配的卡（如卡牌图鉴复用本预制体）不响应指针。
    /// 位置与层序一律经卡牌根下达，能力不直接写 transform 与 SortingGroup；指针世界坐标由宿主提供，无宿主时静默忽略，不报错。</summary>
    [RequireComponent(typeof(Obj_Card))]
    public class Comp_CardDrag : MonoBehaviour, ICardAbility, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        // 拖拽期间固定在最高层序：压过手牌排布、出牌区与悬停（90），保证抓在手里的牌永远在最上层
        private const int DragOrder = 100;

        private Obj_Card _card;
        private Vector3 _grabOffset;
        private bool _dragging;

        // 认领卡根；指针世界坐标改用宿主提供的换算，无需相机
        public void Attach(Obj_Card card_) => _card = card_;

        #region 指针事件：按下抓起抬高、拖动跟手、松手交宿主判定落点

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_card.Host == null) return;  // 无宿主即纯展示卡（如卡牌图鉴复用本预制体），不响应拖拽
            _dragging = true;
            _grabOffset = _card.Host.PointerWorldPosition() - transform.position;
            _card.Host.OnCardGrabbed(_card);
            _card.Place(transform.position, transform.rotation, DragOrder);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _card.Place(_card.Host.PointerWorldPosition() - _grabOffset, transform.rotation, DragOrder);
        }

        // 松手：落点判定交宿主（以卡牌根位置点判出牌区），本能力不再传坐标
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            _card.Host.OnCardReleased(_card);
        }

        #endregion

        // 拖动中被禁用（回收 / 关闭）时直接落回槽位，不停在半途
        private void OnDisable()
        {
            if (!_dragging) return;
            _dragging = false;
            _card.Host.OnCardDragCancelled(_card);
        }
    }
}
