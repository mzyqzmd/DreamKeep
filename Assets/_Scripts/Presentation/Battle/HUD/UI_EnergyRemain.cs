using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>剩余能量视图：订阅能量变化事件把「当前/上限」写到文本上，回能、消耗与获得能量都即时刷新。
    /// 不轮询、不读战斗态，能量何时变化一律由域侧决定。</summary>
    public sealed class UI_EnergyRemain : MonoBehaviour
    {
        [Tooltip("剩余能量文本：格式为「当前/上限」，如 3/3")] [SerializeField]
        private TextMeshProUGUI _remainEnergyText;

        [SerializeField] private Color _exhaustedColor;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeAlpha = 0.5f;

        private Color _originColor;

        #region 接线：能量变化事件驱动文本刷新

        [Inject]
        public void Construct(BattleEventDispatcher eventDispatcher_)
        {
            _originColor = _remainEnergyText.color;
            eventDispatcher_.Register<BE_EnergyChanged>(OnEnergyChangedAsync);
        }

        #endregion

        private UniTask OnEnergyChangedAsync(BE_EnergyChanged changed_)
        {
            int current = Mathf.Max(0, changed_.Current);
            if (current == 0)
            {
                _canvasGroup.alpha = _fadeAlpha;
                _remainEnergyText.color = _exhaustedColor;
            }
            else
            {
                _canvasGroup.alpha = 1f;
                _remainEnergyText.color = _originColor;
            }

            _remainEnergyText.text = $"{current}/{changed_.Max}";
            return UniTask.CompletedTask;
        }
    }
}