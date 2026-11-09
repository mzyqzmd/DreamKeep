using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>整局顶部状态栏：run 装配完成后自动显示（主菜单兼作 run 基地）。
    /// 订阅整局数据变更（HP / 金币 / 层数）刷新对应文本，而非每帧轮询；已用时长由每帧喂增量给 RunState 后读值展示。
    /// 地图与卡组按钮已接到各自面板的开合；菜单按钮开合暂停面板。</summary>
    public sealed class UI_TopBar : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _goldText;

        [Tooltip("金币跳变时长（秒）：数字从旧值滚到新值，0 即瞬间写入")] [SerializeField]
        private float _goldTickDuration = 0.35f;

        [SerializeField] private UI_LayerBar _layerBar;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private Button _mapButton;
        [SerializeField] private Button _deckButton;
        [SerializeField] private Button _menuButton;


        [SerializeField] private UI_MapPanel _mapPanel;

        [SerializeField] private UI_DeckPanel _deckPanel;
        [SerializeField] private UI_PausePanel _pausePanel;

        private RunState _run;
        private int _lastShownSeconds = -1;
        private Tween _goldTween;
        private int _shownGold;
        private bool _goldInitialized;

        [Inject]
        public void Construct(RunState run_)
        {
            _run = run_;

            // 初值：装配完成时直接读一次，后续靠事件增量刷新
            RefreshHp();
            RefreshGold();
            if (_layerBar) _layerBar.SetFloor(_run.Floor, _run.Map.CurrentNode);

            // 订阅整局数据变更（事件驱动，避免每帧轮询）
            _run.Player.Creature.HpChanged += OnHpChanged;
            _run.GoldChanged += OnGoldChanged;
            _run.FloorChanged += OnFloorChanged;

            if (_mapPanel) _mapPanel.HideImmediate(); // 装配时立即收起，不起收起动画、不留透明激活层
            WireButtons();

            // 计时累计在 RunState（构造即从 0 起），这里只把展示缓存归零后写一次初值
            _lastShownSeconds = -1;
            UpdateTime();
        }

        private void OnDestroy()
        {
            _goldTween?.Kill(); // 顶栏随整局常驻，补间兜底在自己身上收
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
            if (_layerBar) _layerBar.SetFloor(floor_, _run.Map.CurrentNode);
        }

        private void RefreshHp()
        {
            if (_hpText == null) return;
            Creature creature = _run.Player.Creature;
            _hpText.text = $"{creature.CurrentHp} / {creature.MaxHp}";
        }

        // 金币：首帧直接写初值，之后每次变更从当前显示值滚到新值（连续买卖时从中途续跑，不叠加、不倒退）
        private void RefreshGold()
        {
            if (_goldText == null) return;
            if (!_goldInitialized)
            {
                _goldInitialized = true;
                WriteGold(_run.Gold);
                return;
            }

            if (_goldTickDuration <= 0f)
            {
                WriteGold(_run.Gold);
                return;
            }

            UiTween.PlayScalar(ref _goldTween, () => _shownGold, value_ => WriteGold(Mathf.RoundToInt(value_)),
                _run.Gold, _goldTickDuration).OnComplete(() => WriteGold(_run.Gold));
        }

        // 只在整数真的变了才写 TMP：补间每帧都会回调，重复赋值会白白触发文字重排
        private void WriteGold(int value_)
        {
            if (_shownGold == value_) return;
            _shownGold = value_;
            _goldText.text = value_.ToString();
        }

        #endregion

        #region 顶栏按钮：地图 / 卡组 / 暂停三方互斥开合

        private void WireButtons()
        {
            if (_mapButton) _mapButton.onClick.AddListener(ToggleMap);
            if (_deckButton) _deckButton.onClick.AddListener(ToggleDeck);
            if (_menuButton) _menuButton.onClick.AddListener(TogglePanel);
        }

        /// <summary>打开地图面板：段收尾（休息/先古/战斗奖励的「继续」）与顶栏按钮共用这一个入口。</summary>
        public void OpenMap()
        {
            if (_mapPanel) _mapPanel.Show();
        }

        /// <summary>收起地图面板：段加载器在选定下一个节点、装载新段前调用，避免遮罩盖住新段。</summary>
        public void CloseMap()
        {
            // 切段必须立即收图：开图动画期间选点时 Hide 会被 _switching 吞掉，透明遮罩会盖住新段并吃掉点击
            if (_mapPanel) _mapPanel.HideImmediate();
        }

        private void ToggleMap()
        {
            if (_mapPanel == null)
            {
                Debug.Log("地图面板未接线");
                return;
            }
            if (_deckPanel && _deckPanel.gameObject.activeSelf) _deckPanel.Hide(); // 面板互斥：开图关卡组
            ClosePause();
            _mapPanel.Toggle();
        }

        // 卡组面板开关：与地图、暂停互斥（开组关图与菜单）；面板尚未摆进场景时引用为空，点击仅作空监听
        private void ToggleDeck()
        {
            if (_deckPanel == null)
            {
                Debug.Log("卡组面板未接线");
                return;
            }
            if (_mapPanel && _mapPanel.gameObject.activeSelf) _mapPanel.Hide();
            ClosePause();
            _deckPanel.Toggle();
        }

        // 菜单按钮开合暂停面板：开面板前先收地图与卡组，三层全屏遮罩不同时叠
        private void TogglePanel()
        {
            if (_pausePanel == null)
            {
                Debug.Log("暂停面板未接线");
                return;
            }
            if (!_pausePanel.gameObject.activeSelf)
            {
                if (_mapPanel && _mapPanel.gameObject.activeSelf) _mapPanel.Hide();
                if (_deckPanel && _deckPanel.gameObject.activeSelf) _deckPanel.Hide();
            }
            _pausePanel.Toggle();
        }

        /// <summary>收起暂停面板：放弃/设置链与回主菜单清理前调用，避免遮罩叠层。</summary>
        public void ClosePause()
        {
            if (_pausePanel && _pausePanel.gameObject.activeSelf) _pausePanel.Hide();
        }

        #endregion

        #region 计时展示：本类兼任整局计时的泵，仅整秒跳变时重建字符串

        // 计时口径：顶栏在场 = 整局计时在跑。全库只有 SegmentSceneManager.PrepareReturnToMenuAsync
        // 会让顶栏失活，那一刻本局已结束、正回主菜单，停表符合预期；换段与开地图期间顶栏保持激活，照常累计。
        private void Update()
        {
            if (_run == null) return;
            _run.TickTime(Time.unscaledDeltaTime); // 真实时间口径：不受 timeScale 影响，与原秒表语义一致
            UpdateTime();
        }

        private void UpdateTime()
        {
            if (_timeText == null) return;
            int seconds = (int)_run.ElapsedSeconds;
            if (seconds == _lastShownSeconds) return;
            _lastShownSeconds = seconds;
            _timeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        #endregion
    }
}