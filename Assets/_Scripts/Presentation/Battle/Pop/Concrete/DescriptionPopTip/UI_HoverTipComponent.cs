using _DreamKeep.Utility;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    public sealed class UI_HoverTipComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private EUiAnchor _startAnchor;
        [SerializeField] private EUiAnchor _tipAnchor;
        [SerializeField] private Vector2 _tipOffsetPx;

        [SerializeField] private bool _isStaticContents;

        [ShowIf("_isStaticContents"), SerializeField]
        private string _tipName;

        [ShowIf("_isStaticContents"), SerializeField]
        private string _tipDescription;

        public void OnPointerEnter(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }
    }
}