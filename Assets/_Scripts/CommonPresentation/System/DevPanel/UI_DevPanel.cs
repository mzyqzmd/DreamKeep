using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>局内测试浮窗：激活失活由整局编排者按测试模式开关直接控制，本类只管拖拽、折叠与七个按钮；
    /// 按钮改当前整局数值或秒掉本场怪，不写盘。血条补刷走装配期注入的委托，不播回血特效。</summary>
    public sealed class UI_DevPanel : UI_Panel
    {
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private RectTransform _dragPoint;
        [SerializeField] private Button _foldButton;
        [SerializeField] private TextMeshProUGUI _foldLabel;
        [SerializeField] private RectTransform _foldArrow;
        [SerializeField] private GameObject _contentPanel;
        [SerializeField] private Button _raiseMaxHealthButton;
        [SerializeField] private Button _decreaseMaxHealthButton;
        [SerializeField] private Button _healButton;
        [SerializeField] private Button _loseHealthButton;
        [SerializeField] private Button _gainGoldButton;
        [SerializeField] private Button _loseGoldButton;
        [SerializeField] private Button _passBattleButton;

        private RunState _run;
        private BattleSession _session;
        private Action<Creature> _refreshHealthBar;
        private Vector2 _pointerOrigin;
        private Vector2 _panelOrigin;

        private Creature Player => _run.Player.Creature;

        private const int MaxHealthStep = 10;
        private const int HealthStep = 10;
        private const int GoldGain = 100;
        private const int GoldLoss = 100;

        private void Awake()
        {
            WireControls();
            WireDrag();
        }

        [Inject]
        public void Construct(RunState runState_, BattleSession battleSession_, Action<Creature> refreshHealthBar_)
        {
            _run = runState_;
            _session = battleSession_;
            _refreshHealthBar = refreshHealthBar_;
        }

        private void WireControls()
        {
            if (_foldButton) _foldButton.onClick.AddListener(OnFoldClicked);
            if (_raiseMaxHealthButton) _raiseMaxHealthButton.onClick.AddListener(OnRaiseMaxHealth);
            if (_decreaseMaxHealthButton) _decreaseMaxHealthButton.onClick.AddListener(OnDecreaseMaxHealth);
            if (_healButton) _healButton.onClick.AddListener(OnHeal);
            if (_loseHealthButton) _loseHealthButton.onClick.AddListener(OnLoseHealth);
            if (_gainGoldButton) _gainGoldButton.onClick.AddListener(OnGainGold);
            if (_loseGoldButton) _loseGoldButton.onClick.AddListener(OnLoseGold);
            if (_passBattleButton) _passBattleButton.onClick.AddListener(OnPassBattle);
        }

        private void WireDrag()
        {
            if (!_dragPoint || !_panelRect) return;
            EventTrigger trigger = _dragPoint.GetComponent<EventTrigger>();
            if (!trigger) trigger = _dragPoint.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry begin = new() { eventID = EventTriggerType.BeginDrag };
            begin.callback.AddListener(OnBeginDrag);
            trigger.triggers.Add(begin);

            EventTrigger.Entry drag = new() { eventID = EventTriggerType.Drag };
            drag.callback.AddListener(OnDrag);
            trigger.triggers.Add(drag);
        }

        private void OnBeginDrag(BaseEventData data_)
        {
            if (data_ is not PointerEventData pointer) return;
            _panelOrigin = _panelRect.anchoredPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_panelRect.parent,
                pointer.position, pointer.pressEventCamera, out _pointerOrigin);
        }

        private void OnDrag(BaseEventData data_)
        {
            if (data_ is not PointerEventData pointer) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_panelRect.parent,
                    pointer.position, pointer.pressEventCamera, out Vector2 point_))
                _panelRect.anchoredPosition = _panelOrigin + (point_ - _pointerOrigin);
        }

        private void OnFoldClicked()
        {
            bool expand = !_contentPanel.activeSelf;
            _contentPanel.SetActive(expand);
            if (_foldLabel) _foldLabel.text = expand ? "收起Dev面板" : "打开Dev面板";
            if (_foldArrow) _foldArrow.localRotation = Quaternion.Euler(0f, 0f, expand ? 90f : -90f);
        }

        // 顶栏随 HpChanged 自动刷新；战斗内血条由注入委托直刷，不补发会播特效的回血事件
        private void RefreshHealthBar() => _refreshHealthBar?.Invoke(Player);

        private void OnRaiseMaxHealth()
        {
            Player.RaiseMaxHp(MaxHealthStep);
            RefreshHealthBar();
        }

        private void OnDecreaseMaxHealth()
        {
            Player.LowerMaxHp(MaxHealthStep);
            RefreshHealthBar();
        }

        private void OnHeal()
        {
            Player.CurrentHp = Mathf.Min(Player.CurrentHp + HealthStep, Player.MaxHp);
            RefreshHealthBar();
        }

        private void OnLoseHealth()
        {
            Player.CurrentHp = Mathf.Max(1, Player.CurrentHp - HealthStep);
            RefreshHealthBar();
        }

        private void OnGainGold() => _run.RestoreGoldTo(_run.Gold + GoldGain);

        private void OnLoseGold() => _run.RestoreGoldTo(Mathf.Max(0, _run.Gold - GoldLoss));

        // 直写敌人血 0 让 IsVictory 成立，再入队 EndTurn 触发胜负判定；不发死亡事件，敌人不播倒地演出
        private void OnPassBattle()
        {
            if (!_session.IsOpen) return;
            foreach (Creature enemy in _session.Query.GetAliveEnemies()) enemy.CurrentHp = 0;
            _session.Command.EndTurn();
        }
    }
}