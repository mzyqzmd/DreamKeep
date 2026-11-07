using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.EngineUtility;
using _DreamKeep.Utility;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗弹层编排：挂在场景 GameRoot 上，只决定"谁在什么时候弹什么、弹在哪"——
    /// 订阅伤害/回血/回合开始与出牌被拒/手牌溢出/敌人出招，按语义选文本、颜色与形态，算出挂点后交给通用弹层工厂 PopFactory；
    /// 说明气泡的实例取还与列守卫、数字飘字的池、对话气泡的左右池、通用横幅公告都归 PopFactory。
    /// 弹层一律挂到"所属对象"之下（数字与台词气泡→单位视图），只有横幅由本组件指定挂载父级后交工厂触发。</summary>
    public sealed class UI_BattlePopRoot : MonoBehaviour
    {
        [Header("数字飘字")] [SerializeField] private Color _damageColor = new(1f, 0.28f, 0.22f);

        [Tooltip("失去生命的飘字色：比伤害更深，二者靠颜色与文案区分")] [SerializeField]
        private Color _lostHealthColor = new(0.62f, 0.05f, 0.05f);

        [SerializeField] private Color _healColor = new(0.35f, 0.92f, 0.45f);
        [SerializeField] private Color _blockColor = new(0.55f, 0.82f, 1f);

        [Tooltip("相对骨架顶沿的抬升（画布像素）")] [SerializeField]
        private float _liftPx = 20f;

        [Tooltip("数字起点的横向随机抖动（画布像素）：多段伤害同时出现时错开")] [SerializeField]
        private float _jitterPx = 18f;

        [Header("回合横幅")] [Tooltip("回合横幅的挂载父级：它没有所属对象，只能在这里指定")] [SerializeField]
        private Transform _turnBannerParent;

        [Header("对话气泡")] [Tooltip("台词表：玩家出牌被拒与手牌溢出时说的话")] [SerializeField]
        private SO_PlayerSpeechLines _playerSpeechLines;

        [Tooltip("气泡锚点相对骨架顶沿的抬升（画布像素）")] [SerializeField]
        private float _speechLiftPx = 24f;

        private UI_ActorRoot _actorRoot;
        private BattleContext _battleContext;
        private PopFactory _popFactory;

        #region 装配：注入事件门面、单位视图登记表与通用弹层工厂

        [Inject]
        public void Construct(BattleEventDispatcher dispatcher_, UI_ActorRoot actorRoot_, PopFactory popFactory_,
            BattleContext battleContext_)
        {
            _actorRoot = actorRoot_;
            _popFactory = popFactory_;
            _battleContext = battleContext_;
            dispatcher_.Register<BE_DamageDealt>(OnDamageDealt);
            dispatcher_.Register<BE_HealthLost>(OnHealthLost);
            dispatcher_.Register<BE_HealthRestored>(OnHealthRestored);
            dispatcher_.Register<BE_TurnStarted>(OnTurnStarted);
            dispatcher_.Register<BE_CardPlayRejected>(OnCardPlayRejected);
            dispatcher_.Register<BE_HandOverflowed>(OnHandOverflowed);
            dispatcher_.Register<BE_EnemySpoke>(OnEnemySpoke);
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

        // 失去生命：无视格挡直接掉血，文案带「失去」与深色，避免与伤害数字混淆
        private void OnHealthLost(BE_HealthLost lost_) =>
            ShowNumber($"失去 {lost_.Amount}", _lostHealthColor, ENumberPopTipType.Toss, lost_.Target);

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

        // 头顶锚点：骨架矩形正中心再抬高一段，横向抖动沿骨架自身轴向换算
        private Vector3 AnchorOf(UI_Actor view_)
        {
            RectTransform body = (RectTransform)view_.Skeleton.transform;
            float jitter = UnityEngine.Random.Range(-_jitterPx, _jitterPx);
            return UiGeometry.AnchorPoint(body, EUiAnchor.MiddleCenter) +
                   body.up * (_liftPx * body.lossyScale.y) +
                   body.right * (jitter * body.lossyScale.x);
        }

        // 回合横幅：它没有所属对象，挂到 Inspector 指定的父级；经通用弹层工厂 PopFactory 触发（单实例、重复触发由横幅自身重播时间线）
        private void OnTurnStarted(BE_TurnStarted started_)
        {
            _popFactory.PopBanner(_turnBannerParent, started_.IsPlayerTurn ? "玩家回合" : "敌人回合",
                started_.IsPlayerTurn ? $"第 {started_.TurnCount} 回合" : "");
        }

        // 出牌被拒：只有能量不足/不能打出/昏眩限牌让玩家开口；牌已离手属时序竞争，保持沉默
        private void OnCardPlayRejected(BE_CardPlayRejected rejected_)
        {
            string line = rejected_.Reason switch
            {
                ECardPlayRejectReason.NotEnoughEnergy => _playerSpeechLines.PickNotEnoughEnergy(),
                ECardPlayRejectReason.Unplayable => _playerSpeechLines.PickUnplayable(),
                ECardPlayRejectReason.DizzyLimited => _playerSpeechLines.PickDizzyLimited(),
                _ => null,
            };
            SpeakPlayer(line);
        }

        // 手牌溢出：抽到的牌直接进弃牌堆，玩家说一句；同侧气泡在播时由工厂丢弃
        private void OnHandOverflowed(BE_HandOverflowed _) => SpeakPlayer(_playerSpeechLines.PickHandOverflow());

        // 敌人出招：意图执行前弹 R 气泡，挂到敌人视图下随其移动与销毁
        private void OnEnemySpoke(BE_EnemySpoke spoke_) =>
            Speak(spoke_.Enemy.Creature, ESpeechBubbleSide.Right, spoke_.Line);

        private void SpeakPlayer(string line_) => Speak(_battleContext.Player.Creature, ESpeechBubbleSide.Left, line_);

        private void Speak(Creature speaker_, ESpeechBubbleSide side_, string line_)
        {
            if (string.IsNullOrEmpty(line_)) return;
            if (!_actorRoot.TryGetView(speaker_, out UI_Actor view)) return;
            _popFactory.Speak(side_, view.transform, SpeechAnchorOf(view), line_);
        }

        // 气泡锚点：骨架中心往上抬身高的三成（近似头部）再按 Inspector 微调；
        // 不取矩形顶沿——长武器与角会把顶沿抬到离头很远的位置
        private Vector3 SpeechAnchorOf(UI_Actor view_)
        {
            RectTransform body = (RectTransform)view_.Skeleton.transform;
            return UiGeometry.AnchorPoint(body, EUiAnchor.MiddleCenter) +
                   body.up * ((body.rect.height * 0.3f + _speechLiftPx) * body.lossyScale.y);
        }
    }
}