using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace _DreamKeep.Presentation
{
    /// <summary>战场单位区：持有纵深阵型四个槽位点（0 号为玩家、1~3 号为敌人，1 最靠前、3 最靠后），是单位出生摆位与
    /// 移动补间的唯一坐标来源；同时内嵌"生物 → 单位视图"登记表（单位装配时自登记、销毁时注销），并每帧裁决指针悬停在
    /// 哪个单位上——命中范围与目标箭头同一套几何，被其他界面压住时不算，变化才下发名牌与描边高亮。</summary>
    public sealed class UI_ActorRoot : MonoBehaviour
    {
        [SerializeField] private Transform _slot0;
        [SerializeField] private Transform _slot1;
        [SerializeField] private Transform _slot2;
        [SerializeField] private Transform _slot3;

        private readonly Dictionary<Creature, UI_Actor> _views = new();

        // 逐帧射线裁决的复用件：命中列表与指针数据只建一次，避免每帧产生垃圾
        private readonly List<RaycastResult> _raycastResults = new();
        private Canvas _canvas;
        private PointerEventData _pointerData;
        private EventSystem _pointerEventSystem;
        private UI_Actor _hovered;

        private void Awake() => _canvas = GetComponentInParent<Canvas>();

        // 每帧裁一次悬停：指针换算 → 取指针下的存活单位（多个命中取登记序靠后者，即层级更靠前）→ 射线排除遮挡 → 差分下发
        private void Update()
        {
            if (!_hovered) _hovered = null; // 视图随死亡自毁后只留下伪空引用，先归零再重新裁

            UI_Actor target = null;
            if (Pointer.current != null && _canvas != null)
            {
                Vector3 pointerWorld = PointerWorldPosition();
                foreach (KeyValuePair<Creature, UI_Actor> pair in _views)
                {
                    if (pair.Key.IsDead || !pair.Value) continue;
                    if (pair.Value.ContainsWorldPoint(pointerWorld)) target = pair.Value;
                }

                if (target && IsObscured(target)) target = null;
            }

            if (target == _hovered) return;
            if (_hovered) _hovered.SetHovered(false);
            _hovered = target;
            if (_hovered) _hovered.SetHovered(true);
        }

        // 指针在画布平面上的世界坐标：与手牌区同一套换算（画布是 ScreenSpaceCamera，必须传画布相机）
        private Vector3 PointerWorldPosition()
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)_canvas.transform,
                Pointer.current.position.ReadValue(), _canvas.worldCamera, out Vector3 world);
            return world;
        }

        // 遮挡排除：指针下最上层可交互元素不属于该单位自己的层级时算被压住（手牌、牌堆、按钮等）；
        // 单位自己的界面（血条、状态槽、意图图标、提示框）压在指针上仍算悬停本单位
        private bool IsObscured(UI_Actor target_)
        {
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem) return false;
            if (_pointerEventSystem != eventSystem)
            {
                _pointerEventSystem = eventSystem;
                _pointerData = new PointerEventData(eventSystem);
            }

            _pointerData.position = Pointer.current.position.ReadValue();
            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerData, _raycastResults);
            return _raycastResults.Count > 0 && !_raycastResults[0].gameObject.transform.IsChildOf(target_.transform);
        }

        #region 按槽位号取槽位点：单位出生与移动都向它查询

        public Transform GetSlotTransform(int slot_) => slot_ switch
        {
            0 => _slot0,
            1 => _slot1,
            2 => _slot2,
            3 => _slot3,
            _ => null,
        };

        #endregion

        #region 对外暴露的登记与查询：单位视图装配时自登记、销毁时注销

        public void Register(Creature creature_, UI_Actor view_) => _views[creature_] = view_;

        public void Unregister(Creature creature_) => _views.Remove(creature_);

        public bool TryGetView(Creature creature_, out UI_Actor view_) => _views.TryGetValue(creature_, out view_);

        #endregion
    }
}
