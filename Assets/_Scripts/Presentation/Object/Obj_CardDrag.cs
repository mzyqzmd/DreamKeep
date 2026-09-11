using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>卡牌拖拽输入：按下抓取、拖动跟随指针、松手交回手牌视图判定，由手牌视图注入宿主。</summary>
    public class Obj_CardDrag : MonoBehaviour
    {
        private Camera _camera;
        private Obj_Card _card;
        private Obj_HandView _handView;
        private Vector3 _grabOffset;
        private bool _dragging;

        private void Awake()
        {
            _camera = Camera.main;
            _card = GetComponent<Obj_Card>();
        }

        #region 注入宿主手牌视图
        public void Initialize(Obj_HandView handView_) => _handView = handView_;
        #endregion

        private void OnMouseDown()
        {
            _dragging = true;
            _grabOffset = transform.position - PointerWorldPosition();
            _handView.BeginDrag(_card);
        }

        private void OnMouseDrag()
        {
            if (!_dragging) return;
            _handView.UpdateDrag(_card, PointerWorldPosition() + _grabOffset);
        }

        private void OnMouseUp()
        {
            if (!_dragging) return;
            _dragging = false;
            _handView.EndDrag(_card);
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
