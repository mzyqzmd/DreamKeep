using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>结束回合按钮：指针进入激活发光边框、移出即熄灭，点击转发结束回合命令，文案随回合推进刷新为「结束第 n 回合」。
    /// 可交互窗口只有玩家回合进行阶段——进入该阶段才恢复本色可点击，点击瞬间立即置灰；
    /// 回合开始（抽牌）、回合结束（结算与弃牌）、敌方回合与战斗结束后，底图与文本一律乘同一灰调且不发光不响应点击，
    /// 否则命令会排在队列里等下一个玩家回合开头立即生效，把回合吞掉。
    /// 指针交互走 UGUI 指针事件（场景 EventSystem + Canvas 上的 Graphic Raycaster），不逐帧轮询。</summary>
    public class UI_EndTurnButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("发光边框：仅在可交互且指针悬停时激活")] [SerializeField]
        private GameObject _glowFrame;

        [Tooltip("按钮文本：每次回合开始时刷新为「结束第 n 回合」")] [SerializeField]
        private TextMeshProUGUI _text;

        [Tooltip("不可交互时整按钮（底图与文本）乘的灰调")] [SerializeField]
        private Color _disabledTint = new(0.45f, 0.45f, 0.5f);

        private BattleCommandDispatcher _commandDispatcher;
        private Image _background;
        private Color _backgroundBase;
        private Color _textBase;
        private bool _interactive;
        private bool _hovered;

        // 采集编辑期本色后按保守初值刷灰：战斗开始前与战斗开始阶段按钮即处于不可交互外观
        private void Awake()
        {
            _background = GetComponent<Image>();
            _backgroundBase = _background.color;
            _textBase = _text.color;
            ApplyLook();
        }

        #region 接线：命令门面转发结束回合意图，事件门面订阅回合开始、操作窗口开启与战斗结束刷新可交互态

        [Inject]
        public void Construct(BattleCommandDispatcher commandDispatcher_, BattleEventDispatcher eventDispatcher_)
        {
            _commandDispatcher = commandDispatcher_;
            eventDispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);
            eventDispatcher_.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStartedAsync);
            eventDispatcher_.Register<BE_BattleEnded>(OnBattleEndedAsync);
        }

        #endregion

        #region 指针事件：悬停开关发光边框，点击在可交互时先本地落闸再转发结束回合命令

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyLook();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            ApplyLook();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactive) return;
            // 本地先落闸：域侧要等弃牌等演出播完、敌方回合开始才关闸，等事件会导致"点完还亮着"
            _interactive = false;
            ApplyLook();
            _commandDispatcher.EndTurn();
        }

        #endregion

        // 外观唯一写入口：底色/文本二选一本色或灰调，发光由可交互与悬停共同裁决
        private void ApplyLook()
        {
            _background.color = _interactive ? _backgroundBase : _backgroundBase * _disabledTint;
            _text.color = _interactive ? _textBase : _textBase * _disabledTint;
            _glowFrame.SetActive(_interactive && _hovered);
        }

        // 回合开始：刷新文案并一律关闸（抽牌演出期间不可点），开闸只由操作窗口开启事件负责
        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            _text.text = $"结束第{started_.TurnCount}回合";
            _interactive = false;
            ApplyLook();
            return UniTask.CompletedTask;
        }

        // 操作窗口开启：玩家回合进行阶段的唯一开闸点
        private UniTask OnPlayerTurnActiveStartedAsync(BE_PlayerTurnActiveStarted started_)
        {
            _interactive = true;
            ApplyLook();
            return UniTask.CompletedTask;
        }

        // 战斗结束：不再有下一个回合，一律不可交互
        private UniTask OnBattleEndedAsync(BE_BattleEnded ended_)
        {
            _interactive = false;
            ApplyLook();
            return UniTask.CompletedTask;
        }
    }
}
