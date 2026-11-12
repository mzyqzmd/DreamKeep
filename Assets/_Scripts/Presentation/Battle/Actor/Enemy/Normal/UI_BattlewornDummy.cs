using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Presentation
{
    /// <summary>训练机器人视图：按装配期皮肤名切 v1/v2/v3 三档外观；致死的这一下先完整播受击再倒地，死亡沿用基类播完即销毁。</summary>
    public sealed class UI_BattlewornDummy : UI_Enemy
    {
        private const string DeathLoopAnim = "dead_loop";

        protected override string AttackAnim => string.Empty;

        protected override string DeathAnim => DeathLoopAnim;

        protected override void OnEnemyBound(BattleEventDispatcher eventDispatcher_) => ApplySkin(_boundEnemy.SkinName);

        // 基类为不拖长事件链跳过致死伤的 hurt；本骨架没有独立 die 过渡，先播满受击再交给 BE_ActorDied 播倒地
        protected override async UniTask OnDamagedAsync(BE_DamageDealt dealt_)
        {
            if (dealt_.Target == _boundCreature && _boundCreature.IsDead && dealt_.Amount > dealt_.Absorbed)
                await _animTool.PlayOnceAsync(HitAnim, playRatio_: 1f);
            await base.OnDamagedAsync(dealt_);
        }

        private void ApplySkin(string skinName_)
        {
            if (string.IsNullOrEmpty(skinName_) || _skeleton == null) return;
            if (!_skeleton.IsValid) _skeleton.Initialize(false);
            if (_skeleton.Skeleton.Data.FindSkin(skinName_) == null) return;
            _skeleton.Skeleton.SetSkin(skinName_);
            _skeleton.Skeleton.SetSlotsToSetupPose();
        }
    }
}
