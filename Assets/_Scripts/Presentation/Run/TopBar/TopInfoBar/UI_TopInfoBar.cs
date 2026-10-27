using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    public sealed class UI_TopInfoBar : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private UI_PotionGroup _potionGroup;
        [SerializeField] private UI_LayerBar _layerBar;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private Button _mapButton;
        [SerializeField] private Button _deckButton;
        [SerializeField] private Button _menuButton;
     
    }
}