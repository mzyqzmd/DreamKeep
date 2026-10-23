using _DreamKeep.CommonPresentation;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.EngineUtility;
using _DreamKeep.Utility;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗弹层编排：挂在场景 GameRoot 上，只决定"谁在什么时候弹什么、弹在哪"——
    /// 订阅伤害/回血/回合开始，按语义选文本、颜色与形态，算出挂点后交给通用弹层工厂 PopFactory；
    /// 说明气泡的实例取还与列守卫、数字飘字的池都归 PopFactory，回合横幅属战斗语义仍由本组件持有。
    /// 弹层一律挂到"所属对象"之下（数字→单位视图），只有回合横幅由 Inspector 配默认父级。</summary>
    public sealed class BattlePopRoot : MonoBehaviour
    {
        [SerializeField] private UI_TurnCountPopTip _turnCountPopTipPrefab;

        [Header("数字飘字")] [SerializeField] private Color _damageColor = new(1f, 0.28f, 0.22f);
        [SerializeField] private Color _healColor = new(0.35f, 0.92f, 0.45f);
        [SerializeField] private Color _blockColor = new(0.55f, 0.82f, 1f);

        [Tooltip("相对骨架顶沿的抬升（画布像素）")] [SerializeField]
        private float _liftPx = 40f;

        [Tooltip("数字起点的横向随机抖动（画布像素）：多段伤害同时出现时错开")] [SerializeField]
        private float _jitterPx = 18f;

        [Header("回合横幅")] [Tooltip("回合横幅的挂载父级：它没有所属对象，只能在这里指定")] [SerializeField]
        private Transform _turnBannerParent;

        private UI_TurnCountPopTip _turnCountPopTip;
        private UI_ActorRoot _actorRoot;
        private PopFactory _popFactory;
        private Rng _rng;

        #region 装配：注入事件门面、单位视图登记表、通用弹层工厂与表现流随机

        [Inject]
        public void Construct(BattleEventDispatcher dispatcher_, UI_ActorRoot actorRoot_, PopFactory popFactory_,
            [Key("Presentation")] Rng rng_)
        {
            _actorRoot = actorRoot_;
            _popFactory = popFactory_;
            _rng = rng_;
            dispatcher_.Register<BE_DamageDealt>(OnDamageDealt);
            dispatcher_.Register<BE_HealthRestored>(OnHealthRestored);
            dispatcher_.Register<BE_TurnStarted>(OnTurnStarted);
        }

        #endregion

        // 受伤：实际掉血弹 Toss 数字；有格挡吸收时另弹「格挡！」的 Rise 文案
        private void OnDamageDealt(BE_DamageDealt dealt_)
        {
            int hpLoss = dealt_.Amount - dealt_.Absorbed;
            if (hpLoss <= 0 && dealt_.Absorbed > 0)
                ShowNumber("格挡！", _blockColor, ENumberPopTipType.Rise, dealt_.Target);
            else
                ShowNumber(hpLoss.ToString(), _damageColor, ENumberPopTipType.Toss, dealt_.Target);
        }

        // 回血：Amount 已是实际回复量（域侧钳到上限），没有真正恢复就不弹数字；回血特效由 VFXRoot 照播，零回复也播
        private void OnHealthRestored(BE_HealthRestored restored_)
        {
            if (restored_.Amount > 0)
                ShowNumber(restored_.Amount.ToString(), _healColor, ENumberPopTipType.Rise, restored_.Target);
        }

        // 数字飘字：算好单位视图与头顶世界坐标后交给通用工厂，实例挂到单位视图下（随其移动与销毁）
        private void ShowNumber(string text_, Color color_, ENumberPopTipType type_, Creature target_)
        {
            if (!_actorRoot.TryGetView(target_, out UI_Actor view)) return;
            _popFactory.SpawnNumber(text_, color_, type_, view.transform, AnchorOf(view));
        }

        // 头顶锚点：骨架矩形顶沿中点再抬高一段，横向抖动沿骨架自身轴向换算
        private Vector3 AnchorOf(UI_Actor view_)
        {
            RectTransform body = (RectTransform)view_.Skeleton.transform;
            float jitter = _rng.Next(-_jitterPx, _jitterPx);
            return UiGeometry.AnchorPoint(body, EUiAnchor.TopCenter) +
                   body.up * (_liftPx * body.lossyScale.y) +
                   body.right * (jitter * body.lossyScale.x);
        }

        // 回合横幅：它没有所属对象，挂到 Inspector 指定的父级；同一时刻只留一个实例，重复触发由横幅自己重播时间线
        private void OnTurnStarted(BE_TurnStarted started_)
        {
            if (_turnCountPopTip == null) _turnCountPopTip = Instantiate(_turnCountPopTipPrefab, _turnBannerParent);
            _turnCountPopTip.transform.SetAsLastSibling();
            _turnCountPopTip.Show(started_.IsPlayerTurn, started_.TurnCount);
        }
    }
}
