using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>数字飘字导演：订阅伤害与回血事件，按语义决定弹什么——实际掉血弹 Toss 数字、被格挡吸收弹 Rise 的「格挡！」、
    /// 治疗弹 Rise 数字；主动获得格挡不弹。自己不持池也不认识单位内部，只向 UI_ActorRoot 反查视图取骨架头顶锚点，
    /// 实例取还与播放归 UI_PopTextRoot。</summary>
    public sealed class UI_NumberPopDirector : MonoBehaviour
    {
        [Header("颜色")] [SerializeField] private Color _damageColor = new(1f, 0.28f, 0.22f);
        [SerializeField] private Color _healColor = new(0.35f, 0.92f, 0.45f);
        [SerializeField] private Color _blockColor = new(0.55f, 0.82f, 1f);

        [Header("落点")] [Tooltip("相对骨架顶沿的抬升（画布像素）")] [SerializeField]
        private float _liftPx = 40f;

        [Tooltip("数字起点的横向随机抖动（画布像素）：多段伤害同时出现时错开")] [SerializeField]
        private float _jitterPx = 18f;

        private UI_PopTextRoot _popRoot;
        private UI_ActorRoot _actorRoot;
        private Rng _rng;

        [Inject]
        public void Construct(BattleEventDispatcher dispatcher_, UI_PopTextRoot popRoot_, UI_ActorRoot actorRoot_,
            [Key("Presentation")] Rng rng_)
        {
            _popRoot = popRoot_;
            _actorRoot = actorRoot_;
            _rng = rng_;
            dispatcher_.Register<BE_DamageDealt>(OnDamageDealtAsync);
            dispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
        }

        // 受伤：实际掉血弹 Toss 数字；有格挡吸收时另弹「格挡！」并向左让开，两条不同轨迹
        private UniTask OnDamageDealtAsync(BE_DamageDealt dealt_)
        {
            if (!_actorRoot.TryGetView(dealt_.Target, out UI_Actor view)) return UniTask.CompletedTask;
            RectTransform body = (RectTransform)view.Skeleton.transform;
            Vector3 anchor = AnchorOf(body);
            int hpLoss = dealt_.Amount - dealt_.Absorbed;

            if (hpLoss <= 0 && dealt_.Absorbed > 0)
                _popRoot.ShowNumber("格挡！", _blockColor, ENumberPopTipType.Rise,
                    Along(body, anchor, _rng.Next(-_jitterPx, _jitterPx)));
            else
                _popRoot.ShowNumber(hpLoss.ToString(), _damageColor, ENumberPopTipType.Toss,
                    Along(body, anchor, _rng.Next(-_jitterPx, _jitterPx)));
            return UniTask.CompletedTask;
        }

        // 回血：域侧不封顶，数字只报真正加上去的那部分；没有实际恢复就不弹
        private UniTask OnHealthRestoredAsync(BE_HealthRestored restored_)
        {
            if (!_actorRoot.TryGetView(restored_.Target, out UI_Actor view)) return UniTask.CompletedTask;
            Creature creature = restored_.Target;
            int actual = Mathf.Min(creature.CurrentHp, creature.MaxHp) -
                         Mathf.Min(creature.CurrentHp - restored_.Amount, creature.MaxHp);
            if (actual <= 0) return UniTask.CompletedTask;

            RectTransform body = (RectTransform)view.Skeleton.transform;
            _popRoot.ShowNumber(actual.ToString(), _healColor, ENumberPopTipType.Rise,
                Along(body, AnchorOf(body), _rng.Next(-_jitterPx, _jitterPx)));
            return UniTask.CompletedTask;
        }

        // 头顶锚点：骨架矩形顶沿中点再抬高一段，抬升按骨架自身缩放换算
        private Vector3 AnchorOf(RectTransform body_) =>
            UiGeometry.AnchorPoint(body_, EUiAnchor.TopCenter) + body_.up * (_liftPx * body_.lossyScale.y);

        // 沿骨架自身横向偏移（画布像素）：左右错开与抖动共用这条通道
        private static Vector3 Along(RectTransform body_, Vector3 anchor_, float offsetPx_) =>
            anchor_ + body_.right * (offsetPx_ * body_.lossyScale.x);
    }
}