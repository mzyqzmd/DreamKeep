using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    public sealed class UI_LayerBar : MonoBehaviour
    {
        [SerializeField] private Image _currentLayerIcon;
        [SerializeField] private TextMeshProUGUI _layerCount;
        [SerializeField] private Image _bossLayerIcon;
    }
}