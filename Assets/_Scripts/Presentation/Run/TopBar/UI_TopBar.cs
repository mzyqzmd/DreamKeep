using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>整局顶部状态栏：run 装配完成后自动显示（主菜单兼作 run 基地）。
    /// 订阅整局数据变更（HP / 金币 / 层数）刷新对应文本，而非每帧轮询；已用时长用轻量计时器显示。
    /// 三按钮接到 Map/Deck/Menu 占位面板（面板逻辑尚未开发，本轮仅接引用、切换显示）。</summary>
    public sealed class UI_TopBar : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private UI_PotionGroup _potionGroup;
        [SerializeField] private UI_LayerBar _layerBar;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private Button _mapButton;
        [SerializeField] private Button _deckButton;
        [SerializeField] private Button _menuButton;


        [Header("占位面板：尚未实现，仅接引用")] [SerializeField]
        private UI_MapPanel _mapPanel;

        [SerializeField] private UI_DeckPanel _deckPanel;
        [SerializeField] private UI_MenuPanel _menuPanel;

        private RunState _run;
        private float _runStart;
        private int _lastShownSeconds = -1;

        [Inject]
        public void Construct(RunState run_)
        {
            _run = run_;

            // 初值：装配完成时直接读一次，后续靠事件增量刷新
            RefreshHp();
            RefreshGold();
            if (_layerBar) _layerBar.SetFloor(_run.Floor);

            // 订阅整局数据变更（事件驱动，避免每帧轮询）
            _run.Player.Creature.HpChanged += OnHpChanged;
            _run.GoldChanged += OnGoldChanged;
            _run.FloorChanged += OnFloorChanged;

            WireButtons();

            // 计时起点 = run 装配完成这一刻；停表钩子待存档系统落地后接入
            _runStart = Time.realtimeSinceStartup;
            _lastShownSeconds = -1;
            UpdateTime();
        }

        private void OnDestroy()
        {
            if (_run == null) return;
            _run.Player.Creature.HpChanged -= OnHpChanged;
            _run.GoldChanged -= OnGoldChanged;
            _run.FloorChanged -= OnFloorChanged;
        }

        #region 事件响应：只改对应文本

        private void OnHpChanged(int _) => RefreshHp();
        private void OnGoldChanged(int _) => RefreshGold();

        private void OnFloorChanged(int floor_)
        {
            if (_layerBar) _layerBar.SetFloor(floor_);
        }

        private void RefreshHp()
        {
            if (_hpText == null) return;
            Creature creature = _run.Player.Creature;
            _hpText.text = $"{creature.CurrentHp} / {creature.MaxHp}";
        }

        private void RefreshGold()
        {
            if (_goldText == null) return;
            _goldText.text = _run.Gold.ToString();
        }

        #endregion

        #region 占位按钮：切占位面板显示，面板逻辑以后做

        private void WireButtons()
        {
            if (_mapButton) _mapButton.onClick.AddListener(TogglePanel);
            if (_deckButton) _deckButton.onClick.AddListener(TogglePanel);
            if (_menuButton) _menuButton.onClick.AddListener(TogglePanel);
        }

        // 占位面板尚未摆进场景时引用为空，点击仅作空监听；将来接真实面板时此钩子即生效
        private void TogglePanel() => Debug.Log("占位按钮");

        #endregion

        #region 轻量计时：仅整秒跳变时重建字符串

        private void Update()
        {
            if (_run == null) return;
            UpdateTime();
        }

        private void UpdateTime()
        {
            if (_timeText == null) return;
            int seconds = (int)(Time.realtimeSinceStartup - _runStart);
            if (seconds == _lastShownSeconds) return;
            _lastShownSeconds = seconds;
            _timeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        #endregion
    }
}