using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>墨宝视图：攻击动画从 attack 与两条专属变体中等概率随抽，每个意图只抽一次；单次播满，多段每波播到 70% 接棒重播。</summary>
    public class UI_Inklet : UI_Enemy
    {
        private const string AttackFastAnim = "attack_fast";
        private const string AttackTripleAnim = "attack_triple";
        private const float MultiHitPlayRatio = 0.7f;

        private string _segmentAnim; // 本段攻击共用的动画名，意图变更时清空
        private bool _segmentMultiHit; // 下一条将要执行的意图是否为多段

        // 装配钩子：校验两条专属动画名，并跟踪域侧广播的下一条意图
        protected override void OnEnemyBound(BattleEventDispatcher eventDispatcher_)
        {
            _animTool.VerifyAnim(AttackFastAnim, AttackTripleAnim);
            eventDispatcher_.Register<BE_EnemyIntentChanged>(OnIntentChanged);
        }

        // 意图变更即开新段：刷新多段标记并清掉上一段抽到的动画
        private void OnIntentChanged(BE_EnemyIntentChanged changed_)
        {
            if (changed_.Enemy != _boundEnemy) return;
            _segmentMultiHit = IsMultiHit(changed_.Intent);
            _segmentAnim = null;
        }

        // 我打人：本段首波抽一次并缓存；单次播满，多段每波播到 70% 放行给下一波接棒重播
        protected override async UniTask OnDealtDamageAsync(BE_DamageDealt dealt_)
        {
            if (_boundCreature.IsDead || dealt_.Source != _boundCreature) return;
            // 三条等概率，允许连抽同一条；不消费整局 Rng（其状态随存档写盘）
            string anim = _segmentAnim ??= Random.Range(0, 3) switch
            {
                0 => AttackAnim,
                1 => AttackFastAnim,
                _ => AttackTripleAnim
            };
            float playRatio = _segmentMultiHit ? MultiHitPlayRatio : 1f;
            await _animTool.PlayOnceAsync(anim, playRatio, () => _animTool.PlayLoop(IdleLoopAnim));
        }

        // 复合意图最多两层子意图，构造与解析器都不保证非空，逐层判空
        private static bool IsMultiHit(EnemyIntent intent_) => intent_ switch
        {
            EI_Damage damage => damage.Times > 1,
            EI_Mixed mixed => (mixed.First != null && IsMultiHit(mixed.First)) ||
                              (mixed.Second != null && IsMultiHit(mixed.Second)),
            _ => false
        };
    }
}