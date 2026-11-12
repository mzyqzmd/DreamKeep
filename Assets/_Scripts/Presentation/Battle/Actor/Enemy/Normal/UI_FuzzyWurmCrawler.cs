using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Presentation
{
    /// <summary>毛绒伏地虫视图：自己给自己力量时整段播吸气并当场鼓起，鼓起期间待机、受击与死亡改用 puffed 动画组；
    /// 一整轮内没再获得力量，下一轮玩家回合开始回落到普通组。攻击恒用公约 attack（骨架无 puffed 版）。</summary>
    public sealed class UI_FuzzyWurmCrawler : UI_Enemy
    {
        private const string InhaleAnim = "inhale";
        private const string IdleLoopPuffedAnim = "idle_loop_puffed";
        private const string HitPuffedAnim = "hurt_puffed";
        private const string DeathPuffedAnim = "die_puffed";

        private bool _puffed;
        private bool _inhaledThisRound;

        protected override string IdleLoopAnim => _puffed ? IdleLoopPuffedAnim : base.IdleLoopAnim;
        protected override string HitAnim => _puffed ? HitPuffedAnim : base.HitAnim;
        protected override string DeathAnim => _puffed ? DeathPuffedAnim : base.DeathAnim;

        // 装配钩子：校验鼓起组与吸气动画名，订阅回合翻牌
        protected override void OnEnemyBound(BattleEventDispatcher eventDispatcher_)
        {
            _animTool.VerifyAnim(InhaleAnim, IdleLoopPuffedAnim, HitPuffedAnim, DeathPuffedAnim);
            eventDispatcher_.Register<BE_TurnStarted>(OnTurnStarted);
        }

        // 自己给自己力量：先置标记再整段播吸气，播完回当前组待机（待机名已按状态解析）
        protected override async UniTask OnPerkApplied(BE_PerkApplied applied_)
        {
            await base.OnPerkApplied(applied_);
            if (applied_.Source != _boundCreature || applied_.Target != _boundCreature) return;
            if (!(applied_.Perk is BP_Strength)) return;
            _puffed = true;
            _inhaledThisRound = true;
            await _animTool.PlayOnceAsync(InhaleAnim, 1f, () => _animTool.PlayLoop(IdleLoopAnim));
        }

        // 回合翻牌：只在玩家回合开始消费上一轮吸气标记，敌人回合那次跳过；状态真变化才换待机
        private void OnTurnStarted(BE_TurnStarted started_)
        {
            if (!started_.IsPlayerTurn || _boundCreature.IsDead) return;
            bool keepPuffed = _inhaledThisRound;
            _inhaledThisRound = false;
            if (keepPuffed == _puffed) return;
            _puffed = keepPuffed;
            _animTool.PlayLoop(IdleLoopAnim);
        }
    }
}
