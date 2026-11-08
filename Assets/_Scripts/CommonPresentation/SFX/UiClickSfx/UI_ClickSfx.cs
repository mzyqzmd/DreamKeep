using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>全局 UI 点击音（挂常驻 EventSystem）：按下瞬间做一次与 UGUI 同源的命中判定，
    /// 命中可交互的 Selectable（Button / Toggle / Dropdown）或可交互的 UI_Button 就播一次 UiClick，其余点击一律静默。
    /// 判定走 ExecuteEvents.GetEventHandler 沿父链取真正的点击主体，因此点在按钮文字/图标上也正确；
    /// 子树内挂 UI_ClickSfxIgnore 即整片静音。不绑定任何 onClick，新增 UI 自动生效。</summary>
    [RequireComponent(typeof(EventSystem))]
    public sealed class UI_ClickSfx : MonoBehaviour
    {
        private readonly List<RaycastResult> _hits = new();
        private EventSystem _eventSystem;
        private PointerEventData _pointerData;
        private AudioRoot _audio;

        private void Awake()
        {
            _eventSystem = GetComponent<EventSystem>();
            _pointerData = new PointerEventData(_eventSystem);
        }

        // 只在按下帧做一次射线：未按下立即返回；命中主体与 UGUI 派发用同一套父链查找
        private void Update()
        {
            if (!_audio) return;
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            _pointerData.position = pointer.position.ReadValue();
            _hits.Clear();
            _eventSystem.RaycastAll(_pointerData, _hits);
            if (_hits.Count == 0) return;

            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(_hits[0].gameObject);
            if (!handler || !IsClickable(handler)) return;
            _audio.Play(_audio.Cues.UiClick);
        }

        /// <summary>由常驻装配者注入音频根：拦截器自身不查找服务。</summary>
        public void Bind(AudioRoot audio_) => _audio = audio_;

        // 白名单：父链有静音标记一律不响；其余只认可交互的 Selectable 与可交互的 UI_Button
        private static bool IsClickable(GameObject handler_)
        {
            if (handler_.GetComponentInParent<UI_ClickSfxIgnore>(true)) return false;
            if (handler_.TryGetComponent(out Selectable selectable_)) return selectable_.IsInteractable();
            if (handler_.TryGetComponent(out UI_Button button_)) return button_.Interactable;
            return false;
        }
    }
}
