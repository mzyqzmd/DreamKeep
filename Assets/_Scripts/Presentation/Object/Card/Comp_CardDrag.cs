using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>卡牌拖拽能力：按下抓起抬高、拖动跟手、松手把落点交给宿主判定（打出或回位）；卡牌上挂了该组件才有拖拽效果，展示卡不挂即为纯展示。
    /// 位置与层序一律经卡牌根下达，能力不直接写 transform 与 SortingGroup；该能力只服务战斗手牌，卡牌必有宿主。</summary>
    [RequireComponent(typeof(Obj_Card))]
    public class Comp_CardDrag : MonoBehaviour, ICardAbility, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const int DragOrder = 100;

        private Obj_Card _card;
        private Camera _camera;
        private Vector3 _grabOffset;
        private bool _dragging;

        #region 能力装配：挂载时由卡牌根回调，认领卡牌根

        public void Attach(Obj_Card card_)
        {
            _card = card_;
            _camera = Camera.main;
        }

        #endregion

        #region 指针事件：按下抓起抬高、拖动跟手、松手交宿主判定落点

        public void OnPointerDown(PointerEventData eventData)
        {
            _dragging = true;
            _grabOffset = PointerWorldPosition() - transform.position;
            _card.Host.OnCardGrabbed(_card);
            _card.Place(transform.position, transform.rotation, DragOrder);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _card.Place(PointerWorldPosition() - _grabOffset, transform.rotation, DragOrder);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            _card.Host.OnCardReleased(_card, transform.position);
        }

        #endregion

        // 拖动中被禁用（回收 / 关闭）时直接落回槽位，不停在半途
        private void OnDisable()
        {
            if (!_dragging) return;
            _dragging = false;
            _card.Host.OnCardDragCancelled(_card);
        }

        // 屏幕坐标转世界坐标：深度取卡牌所在平面到相机的距离
        private Vector3 PointerWorldPosition()
        {
            Vector3 pointer = Input.mousePosition;
            pointer.z = -_camera.transform.position.z;
            return _camera.ScreenToWorldPoint(pointer);
        }
    }
}
