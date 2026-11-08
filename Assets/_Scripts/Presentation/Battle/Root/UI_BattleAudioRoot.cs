using _DreamKeep.EngineUtility;
using _DreamKeep.GameBattle;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗音频编排：挂在战斗段根节点上，订阅全部战斗域事件、统一播出音效回执——
    /// 伤害按玩家/敌人分流，回血、格挡、状态施加、抽牌、出牌、被拒、单位死亡、药水使用与回合开合。
    /// 只经 AudioRoot.Play(cue) 发声，cue 全部从 AudioRoot.Cues 取；本类零序列化字段、不认识特效与视图。</summary>
    public sealed class UI_BattleAudioRoot : MonoBehaviour
    {
        private BattleContext _battleContext;
        private AudioRoot _audio;
        private int _lastDrawFrame = -1; // 同帧多张抽牌只响一次，避免叠爆抢占池

        [Inject]
        public void Construct(BattleEventDispatcher dispatcher_, BattleContext battleContext_, AudioRoot audioRoot_)
        {
            _battleContext = battleContext_;
            _audio = audioRoot_;
            dispatcher_.Register<BE_DamageDealt>(OnDamageDealt);
            dispatcher_.Register<BE_HealthRestored>(OnHealthRestored);
            dispatcher_.Register<BE_BlockGained>(OnBlockGained);
            dispatcher_.Register<BE_PerkApplied>(OnPerkApplied);
            dispatcher_.Register<BE_CardDrawn>(OnCardDrawn);
            dispatcher_.Register<BE_CardPlayed>(OnCardPlayed);
            dispatcher_.Register<BE_ActorDied>(OnActorDied);
            dispatcher_.Register<BE_CardPlayRejected>(OnCardPlayRejected);
            dispatcher_.Register<BE_PotionConsumed>(OnPotionConsumed);
            dispatcher_.Register<BE_TurnStarted>(OnTurnStarted);
            dispatcher_.Register<BE_PlayerTurnEnded>(OnPlayerTurnEnded);
        }

        // 受击：格挡全吸收不响；玩家与敌人分流
        private void OnDamageDealt(BE_DamageDealt dealt_)
        {
            if (!_audio || dealt_.Amount <= dealt_.Absorbed) return;
            _audio.Play(dealt_.Target == _battleContext.Player.Creature
                ? _audio.Cues.PlayerHurt : _audio.Cues.Hit);
        }

        private void OnHealthRestored(BE_HealthRestored _) { if (_audio) _audio.Play(_audio.Cues.Heal); }

        private void OnBlockGained(BE_BlockGained _) { if (_audio) _audio.Play(_audio.Cues.Block); }

        private void OnPerkApplied(BE_PerkApplied _) { if (_audio) _audio.Play(_audio.Cues.PerkApply); }

        private void OnCardDrawn(BE_CardDrawn _)
        {
            if (Time.frameCount == _lastDrawFrame) return;
            _lastDrawFrame = Time.frameCount;
            if (_audio) _audio.Play(_audio.Cues.CardDraw);
        }

        private void OnCardPlayed(BE_CardPlayed _) { if (_audio) _audio.Play(_audio.Cues.CardPlay); }

        private void OnActorDied(BE_ActorDied _) { if (_audio) _audio.Play(_audio.Cues.ActorDeath); }

        private void OnCardPlayRejected(BE_CardPlayRejected _) { if (_audio) _audio.Play(_audio.Cues.UiReject); }

        private void OnPotionConsumed(BE_PotionConsumed _) { if (_audio) _audio.Play(_audio.Cues.Potion); }

        private void OnTurnStarted(BE_TurnStarted started_)
        {
            if (_audio && started_.IsPlayerTurn) _audio.Play(_audio.Cues.TurnStart);
        }

        private void OnPlayerTurnEnded(BE_PlayerTurnEnded _) { if (_audio) _audio.Play(_audio.Cues.TurnEnd); }
    }
}
