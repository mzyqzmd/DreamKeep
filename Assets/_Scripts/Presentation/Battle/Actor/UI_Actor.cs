using System;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗单位视图基类：绑定生物、面板装配、共享事件、目标提示、悬停名牌与描边高亮、演出编排（含"演完才落账"的时序）。
    /// 骨架句柄、四条公约动画名与播放原语都在 UI_ActorAnimTool；意图与移动在 UI_Enemy；悬停由 UI_ActorRoot 每帧差分下发。</summary>
    [RequireComponent(typeof(UI_ActorAnimTool))]
    public abstract class UI_Actor : MonoBehaviour
    {
        private const string
            OutlineShaderName = "Spine/Outline/SkeletonGraphic"; // 描边 shader 变体：Inspector 里勾选 Outline 就是换到它

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        [Header("基础配置")] [SerializeField] protected SkeletonGraphic _skeleton; // 演出用骨架
        [SerializeField] private GameObject _targetTip; // 箭头指向本单位时亮起的提示

        [SerializeField] private TextMeshProUGUI _actorName;
        [SerializeField] private Material _skeletonMaterial;
        [SerializeField] protected UI_HealthBar _healthBar;
        [SerializeField] protected UI_BlockView _blockView;
        [SerializeField] protected UI_PerkGroup _perkGroup;
        [Header("悬停反馈")] [SerializeField] private Shader _outlineShader; // 描边 shader 引用，留空则运行时按名查找
        [SerializeField] private float _hoverFadeDuration = 0.12f;
        [SerializeField] private Ease _hoverFadeEase = Ease.OutQuad;
        [Header("死亡配置")] [SerializeField] protected float _deathHoldDuration = 1f; // 死亡停帧：die 播完停末帧这么多秒后自毁视图

        // 公约四条动画名：十条骨架统一导出，全单位一致，写死不可改（将来某单位要做状态变体时再议）
        protected const string IdleLoopAnim = "idle_loop";
        protected const string AttackAnim = "attack";
        protected const string HitAnim = "hurt";
        protected const string DeathAnim = "die";

        public SkeletonGraphic Skeleton => _skeleton;

        /// <summary>绑定的域侧生物：事件按它过滤，血量、格挡与状态真值都从它读。</summary>
        protected Creature _boundCreature;

        /// <summary>战场单位区（槽位坐标 + 视图登记表），装配时向上就近查找。</summary>
        protected UI_ActorRoot _actorRoot;

        protected UI_ActorAnimTool _animTool;
        private bool _targetTipShown;

        // 悬停反馈状态：描边材质副本只在首次悬停克隆、之后常驻，共享材质资产全程不写
        private Material _outlineMaterial;
        private Color _outlineColor;
        private bool _outlineUnavailable;
        private float _hoverFade;
        private Tween _hoverTween;
        private bool _hovered;
        private bool _nameShown;

        /// <summary>已播完死亡动画：此后忽略一切动画请求（托管字段，宿主销毁后仍可安全读）。</summary>
        protected bool _deathAnimTriggered;

        protected void Awake()
        {
            _animTool = GetComponent<UI_ActorAnimTool>();
        }

        // 销毁即收掉悬停补间与描边副本，再注销登记（覆盖敌人死亡自毁这条路径）；子类覆写时先收自己的尾再转调
        protected virtual void OnDestroy()
        {
            _hoverTween?.Kill();
            if (_outlineMaterial) Destroy(_outlineMaterial);
            if (_actorRoot) _actorRoot.Unregister(_boundCreature);
        }

        #region 目标提示：由 UI_TargetTipDirector 按"这张牌会命中的目标"点亮与熄灭

        /// <summary>显示或隐藏目标提示（状态没变就跳过，避免重复 SetActive）。</summary>
        public void ShowTargetTip(bool show_)
        {
            if (!_targetTip || _targetTipShown == show_) return;
            _targetTipShown = show_;
            _targetTip.gameObject.SetActive(show_);
        }

        /// <summary>世界点是否落在本单位命中区内：未接提示时回退判视图根矩形，供箭头命中裁定。</summary>
        public bool ContainsWorldPoint(Vector3 world_) =>
            UiGeometry.Contains((RectTransform)(_targetTip ? _targetTip.transform : transform), world_);

        #endregion

        #region 悬停反馈：指针停在单位范围内时淡入名牌与描边，由 UI_ActorRoot 差分下发

        /// <summary>悬停反馈总开关：显示时淡入名牌与描边，移出时反向淡出；重复同一状态不做任何事。</summary>
        public void SetHovered(bool show_)
        {
            if (_hovered == show_) return;
            _hovered = show_;
            if (show_)
            {
                EnsureOutlineMaterial();
                ShowName();
            }

            _hoverTween?.Kill();
            _hoverTween = DOTween.To(() => _hoverFade, WriteHoverFade, show_ ? 1f : 0f, _hoverFadeDuration)
                .SetEase(_hoverFadeEase)
                .OnComplete(HideNameIfIdle);
        }

        #endregion

        // 渐入系数的唯一写入口：名牌 alpha 与描边颜色按同一系数走。描边 pass 是预乘混合、片元输出整色乘描边遮盖度，
        // 故降不透明度必须整色缩放——只把 a 压到 0 会留下 RGB 加色亮斑
        private void WriteHoverFade(float fade_)
        {
            _hoverFade = fade_;
            if (_actorName) _actorName.alpha = fade_;
            if (_outlineMaterial) _outlineMaterial.SetColor(OutlineColorId, _outlineColor * fade_);
        }

        // 描边材质副本：首次悬停克隆一份并切到描边 shader 变体，赋给骨架后常驻；描边宽度与颜色随克隆自带
        private void EnsureOutlineMaterial()
        {
            if (_outlineMaterial || _outlineUnavailable || !_skeletonMaterial) return;
            Shader shader = _outlineShader ? _outlineShader : Shader.Find(OutlineShaderName);
            if (!shader)
            {
                _outlineUnavailable = true;
                Debug.LogError($"[{GetType().Name}] 找不到描边 shader：{OutlineShaderName}，悬停描边不可用（可在预制体上接 Shader 引用）");
                return;
            }

            _outlineMaterial = new Material(_skeletonMaterial) { shader = shader };
            _outlineColor = _outlineMaterial.GetColor(OutlineColorId);
            _outlineMaterial.SetColor(OutlineColorId, _outlineColor * _hoverFade); // 先抹到当前系数，免得换材质那一帧闪出满描边
            if (_animTool && _skeleton) _skeleton.material = _outlineMaterial;
        }

        // 名牌显隐：亮出前先对齐当前系数（免露出上次残留），淡出补间走完才收起来；补间被杀时不收，留给下一次淡入接续
        private void ShowName()
        {
            if (!_actorName || _nameShown) return;
            _nameShown = true;
            _actorName.alpha = _hoverFade;
            _actorName.gameObject.SetActive(true);
        }

        private void HideNameIfIdle()
        {
            if (!_actorName || _hovered) return;
            _nameShown = false;
            _actorName.gameObject.SetActive(false);
        }

        #region 装配入口：子类绑好自己的生物后转调

        /// <summary>统一装配：绑生物 → 校验公约动画名 → 订阅演出与共享事件 → 初始化面板 → 补建初始状态槽位 → 登记进注册表 → 进入待机循环。</summary>
        protected void Bind(BattleEventDispatcher eventDispatcher_, Creature creature_)
        {
            // 绑定事件和生物属性类
            eventDispatcher_.Register<BE_BattleStarted>(OnBattleStarted);
            eventDispatcher_.Register<BE_ActorDied>(OnActorDiedAsync);
            eventDispatcher_.Register<BE_DamageDealt>(OnDealtDamageAsync);
            eventDispatcher_.Register<BE_DamageDealt>(OnDamagedAsync);
            eventDispatcher_.Register<BE_BlockGained>(OnBlockGainedAsync);
            eventDispatcher_.Register<BE_BlockCleared>(OnBlockClearedAsync);
            eventDispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
            eventDispatcher_.Register<BE_PerkApplied>(OnPerkAppliedAsync);
            eventDispatcher_.Register<BE_PerkDecayed>(OnPerkDecayedAsync);
            eventDispatcher_.Register<BE_PerkRemoved>(OnPerkRemovedAsync);
            _boundCreature = creature_;
            // 把自己注册给_actorRoot
            _actorRoot = GetComponentInParent<UI_ActorRoot>();
            _actorRoot.Register(creature_, this);
            //检查骨骼上是否有动画名
            _animTool.VerifyAnim(IdleLoopAnim, AttackAnim, HitAnim, DeathAnim); // 名字配错就在开局报错，而不是静默播空

            //配置血条格挡信息栏
            _healthBar.Initialize(creature_);
            _blockView.Initialize(creature_);
            // 初始状态随装配自带perk
            foreach (var p in creature_.Powers)
                _perkGroup.AddPerk(p);
        }

        #endregion

        #region 绑定战斗相关事件

        protected virtual UniTask OnBattleStarted(BE_BattleStarted started_)
        {
            if (_boundCreature.IsDead) return UniTask.CompletedTask;
            _animTool.PlayLoop(IdleLoopAnim);
            return UniTask.CompletedTask;
        }

        // 死亡：标记停帧 → 播 die → 停 DeathHoldDuration 秒后自毁视图（等停帧这段不阻塞事件链）
        protected virtual async UniTask OnActorDiedAsync(BE_ActorDied died_)
        {
            if (died_.Creature != _boundCreature || _deathAnimTriggered) return;
            _deathAnimTriggered = true;
            await _animTool.PlayOnceAsync(DeathAnim);
            UniTask.Delay(TimeSpan.FromSeconds(_deathHoldDuration))
                .ContinueWith(() =>
                {
                    if (this != null) Destroy(gameObject);
                })
                .Forget();
        }

        // 我打人：播攻击动画，演完回待机（多段伤害逐段挥拳）
        protected virtual async UniTask OnDealtDamageAsync(BE_DamageDealt dealt_)
        {
            if (_boundCreature.IsDead || dealt_.Source != _boundCreature) return;
            await _animTool.PlayOnceAsync(AttackAnim);
            _animTool.PlayLoop(IdleLoopAnim);
        }

        // 我挨打：播受击动画并刷新血与格挡。致死伤不播（die 由 BE_ActorDied 接管）、未破防无真伤也不播
        protected virtual async UniTask OnDamagedAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Target != _boundCreature) return;
            if (!_boundCreature.IsDead && dealt_.Amount > dealt_.Absorbed)
            {
                await _animTool.PlayOnceAsync(HitAnim);
                if (_boundCreature.IsDead) return;
                _animTool.PlayLoop(IdleLoopAnim);
            }

            // 域侧先扣格挡再扣血才发事件，这里演完再一起对齐真值
            if (_blockView == null || _healthBar == null) return; // 视图可能已随死亡销毁，而托管处理器仍活着
            _blockView.SetBlock(dealt_.Target.Block);
            _healthBar.SetHealth(dealt_.Target.CurrentHp, true);
        }

        // 获得格挡：没有格挡动画，只对齐显示值
        protected virtual UniTask OnBlockGainedAsync(BE_BlockGained gained_)
        {
            if (gained_.Target == _boundCreature && _blockView != null) _blockView.SetBlock(gained_.Target.Block);
            return UniTask.CompletedTask;
        }

        // 回合开始清格挡：同样只对齐显示值
        protected virtual UniTask OnBlockClearedAsync(BE_BlockCleared cleared_)
        {
            if (cleared_.Target == _boundCreature && _blockView != null) _blockView.SetBlock(0);
            return UniTask.CompletedTask;
        }

        // 回血：血条滚到真值
        protected virtual UniTask OnHealthRestoredAsync(BE_HealthRestored restored_)
        {
            if (restored_.Target == _boundCreature && _healthBar != null)
                _healthBar.SetHealth(restored_.Target.CurrentHp, true);
            return UniTask.CompletedTask;
        }

        // 施加状态：新建槽位或叠加层数
        protected virtual UniTask OnPerkAppliedAsync(BE_PerkApplied applied_)
        {
            if (applied_.Target == _boundCreature && _perkGroup != null) _perkGroup.AddPerk(applied_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态层数递减：刷新对应槽位的层数显示
        protected virtual UniTask OnPerkDecayedAsync(BE_PerkDecayed decayed_)
        {
            if (decayed_.Target == _boundCreature && _perkGroup != null) _perkGroup.RefreshPerkLayers(decayed_.Perk);
            return UniTask.CompletedTask;
        }

        // 状态移除：Perk 是被摘下的挂载实例，槽位按状态类型对号删除
        protected virtual UniTask OnPerkRemovedAsync(BE_PerkRemoved removed_)
        {
            if (removed_.Target == _boundCreature && _perkGroup != null) _perkGroup.RemovePerk(removed_.Perk);
            return UniTask.CompletedTask;
        }

        #endregion
    }
}