using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    public sealed class UI_LayerBar : MonoBehaviour
    {
        [SerializeField] private Image _currentLayerOutlineIcon;
        [SerializeField] private Image _currentLayerIcon;
        [SerializeField] private TextMeshProUGUI _layerCount;
        [SerializeField] private Image _bossLayerOutlineIcon;
        [SerializeField] private Image _bossLayerIcon;


        /// <summary>写当前层数文本；Boss 标记随地图系统接入后再驱动。</summary>
        public void SetFloor(int floor_) => _layerCount.text = floor_.ToString();
    }
}