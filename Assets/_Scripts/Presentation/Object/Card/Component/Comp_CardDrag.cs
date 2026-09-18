using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>卡牌拖拽能力：按下抓起抬高、拖动跟手、松手把落点交给宿主判定（打出或回位）；卡牌上配了该组件才有拖拽效果，未配的卡（如卡牌图鉴复用本预制体）不响应指针。
    /// 能否抓起一律问宿主闸门：非我方回合的手牌、飞行中 / 已挂出待结算 / 还没全部抽完落稳的牌都抓不起来。
    /// 抓的是宿主裁决出的"指针下最上层那张能抓的牌"，不一定是收到按下事件的那张——Unity 的 2D 物理射线读不到 SortingGroup 层序，重叠交界处的事件会落到下层牌上。
    /// 位置与层序一律经卡牌根下达，能力不直接写 transform 与 SortingGroup；指针世界坐标由宿主提供，无宿主时静默忽略，不报错。</summary>
    [RequireComponent(typeof(Obj_Card))]
    public class Comp_CardDrag : MonoBehaviour, ICardAbility, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        // 拖拽期间固定在最高层序：压过手牌排布、出牌区与悬停（90），保证抓在手里的牌永远在最上层
        private const int DragOrder = 100;

        private Obj_Card _card;

        // 本次实际被拖的牌：非空即"正在拖"，指针事件始终由收到按下事件的那张牌驱动
        private Obj_Card _dragTarget;
        private Vector3 _grabOffset;

        // 认领卡根；指针世界坐标改用宿主提供的换算，无需相机
        public void Attach(Obj_Card card_) => _card = card_;

        #region 指针事件：按下按宿主裁决抓起指针下最上层那张牌、拖动跟手、松手交宿主判定落点

        public void OnPointerDown(PointerEventData eventData)
        {
            // 无宿主即纯展示卡（如卡牌图鉴复用本预制体）；抓取对象一律由宿主裁决，裁决不出可抓的牌时静默忽略
            if (_card.Host == null || !_card.Host.TryResolveGrab(out Obj_Card target)) return;

            _dragTarget = target;
            _grabOffset = _card.Host.PointerWorldPosition() - target.transform.position;
            _card.Host.OnCardGrabbed(target);
            target.Place(target.transform.position, target.transform.rotation, DragOrder);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragTarget) return;
            _dragTarget.Place(_card.Host.PointerWorldPosition() - _grabOffset, _dragTarget.transform.rotation, DragOrder);
        }

        // 松手：落点判定交宿主（以卡牌根位置点判出牌区），本能力不再传坐标
        public void OnPointerUp(PointerEventData eventData)
        {
            if (_dragTarget) _card.Host.OnCardReleased(TakeDragTarget());
        }

        #endregion

        // 拖动中被禁用（回收 / 关闭）时直接落回槽位，不停在半途；被拖的牌已离手时宿主侧回位是静默空操作
        private void OnDisable()
        {
            if (_dragTarget) _card.Host.OnCardDragCancelled(TakeDragTarget());
        }

        // 交出本次被拖的牌并清空拖拽状态：取完即空，后续指针事件一律静默
        private Obj_Card TakeDragTarget()
        {
            Obj_Card target = _dragTarget;
            _dragTarget = null;
            return target;
        }
    }
}
