using System;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>药水菜单：贴在被点槽位下方，「使用 / 丢弃」两块板各自回一条回调（面板本身不认识药水与战斗）。
    /// 点板面即选中，点面板其余位置不响应；收起由调用方决定（再点槽位、选中任一项、回合窗口关闭）。</summary>
    public sealed class UI_PotionPopup : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("「使用」板：没接时退回第一个子节点")] [SerializeField]
        private RectTransform _usePlate;

        [Tooltip("「丢弃」板：没接时退回第二个子节点")] [SerializeField]
        private RectTransform _discardPlate;

        [Tooltip("相对槽位的像素偏移（画布像素，向右向下为正）")] [SerializeField]
        private Vector2 _offsetPx = new(0f, -80f);

        private Action _onUse;
        private Action _onDiscard;
        private CanvasGroup _useGroup;
        private bool _usable = true;

        private RectTransform UsePlate
        {
            get
            {
                if (!_usePlate) _usePlate = (RectTransform)transform.GetChild(0);
                return _usePlate;
            }
        }

        private RectTransform DiscardPlate
        {
            get
            {
                if (!_discardPlate) _discardPlate = (RectTransform)transform.GetChild(1);
                return _discardPlate;
            }
        }

        // 置灰容器：使用板初始没配，这里自愈补一个，只用来压透明度
        private CanvasGroup UseGroup
        {
            get
            {
                if (_useGroup) return _useGroup;
                _useGroup = UsePlate.GetComponent<CanvasGroup>();
                if (!_useGroup) _useGroup = UsePlate.gameObject.AddComponent<CanvasGroup>();
                return _useGroup;
            }
        }

        #region 弹出与收起：定位在槽位下方，两条回调随本次弹出交出

        public void Show(RectTransform anchor_, bool usable_, Action onUse_, Action onDiscard_)
        {
            _onUse = onUse_;
            _onDiscard = onDiscard_;
            SetUsable(usable_);
            RectTransform rect = (RectTransform)transform;
            Vector3 world = anchor_.position +
                            anchor_.TransformVector(new Vector3(_offsetPx.x, _offsetPx.y, 0f));
            rect.position = UiGeometry.KeepPlane(world, rect.position.z);
            gameObject.SetActive(true);
        }

        /// <summary>使用板是否可用：不可用时压暗且点了不响应（丢弃始终可用）；菜单已弹出时由药水栏在回合切换时刷新。</summary>
        public void SetUsable(bool usable_)
        {
            _usable = usable_;
            UseGroup.alpha = usable_ ? 1f : 0.35f;
        }

        /// <summary>收起：回调一并丢掉，避免下一次弹出前被误用。</summary>
        public void Hide()
        {
            _onUse = null;
            _onDiscard = null;
            gameObject.SetActive(false);
        }

        #endregion

        // 点面板：落在哪块板内就回哪条回调（点的可能是板里的 TMP，指针点击沿层级向上冒泡到本节点）
        public void OnPointerClick(PointerEventData eventData_)
        {
            Action action = ResolveAction(eventData_);
            if (action == null) return;
            Hide();
            action();
        }

        private Action ResolveAction(PointerEventData eventData_)
        {
            Camera camera = eventData_.pressEventCamera;
            if (RectTransformUtility.RectangleContainsScreenPoint(UsePlate, eventData_.position, camera))
                return _usable ? _onUse : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(DiscardPlate, eventData_.position, camera))
                return _onDiscard;
            return null;
        }
    }
}
