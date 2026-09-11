using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _DreamKeep.Data;
using _DreamKeep.Gameplay;
using _DreamKeep.RuntimeData;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>事件播放队列：把一批领域事件压入队列后逐条异步播放，保证同一次结算产生的动作按顺序演完而不是互相覆盖。</summary>
    public class BattlePlaybackQueue : MonoBehaviour
    {
        [SerializeField] private Obj_PlayerView _playerView;

        private readonly Queue<IBattleEvent> _queue = new();

        private BattleState _state;
        private SO_CardDatabase _database;
        private bool _isPlaying;

        #region 注入：由容器在装配期填入战斗态与卡面配置源
        [Inject]
        public void Construct(BattleState state_, SO_CardDatabase database_)
        {
            _state = state_;
            _database = database_;
        }
        #endregion

        #region 压入一批领域事件并启动播放，已在播放时只入队不重复启动
        public void Enqueue(List<IBattleEvent> events_)
        {
            for (int i = 0; i < events_.Count; i++)
                _queue.Enqueue(events_[i]);
            if (!_isPlaying) PlayAll().Forget();
        }
        #endregion

        private async UniTaskVoid PlayAll()
        {
            _isPlaying = true;
            while (_queue.Count > 0)
                await PlayOne(_queue.Dequeue());
            _isPlaying = false;
        }

        private async UniTask PlayOne(IBattleEvent domainEvent_)
        {
            switch (domainEvent_)
            {
                case IBE_BattleStarted:
                    await _playerView.PlayWake();
                    break;
                case IBE_CardPlayed played:
                    await PlayCardAct(played.Card);
                    break;
                case IBE_DamageDealt damage when IsPlayer(damage.Target):
                    await _playerView.PlayHurt();
                    break;
                case IBE_BlockGained block when IsPlayer(block.Target):
                    await _playerView.PlayBlock();
                    break;
                case IBE_BlockCleared cleared when IsPlayer(cleared.Target):
                    _playerView.PlayLeaveBlock();
                    break;
                case IBE_ActorDied died when IsPlayer(died.Actor):
                    await _playerView.PlayDeath();
                    break;
                case IBE_BattleEnded when !_state.Player.IsDead:
                    _playerView.PlaySleep();
                    break;
            }
        }

        private UniTask PlayCardAct(CardModel card_)
        {
            _database.TryGetCard(card_.CardId, out SO_Card config);
            return _playerView.PlayCard(card_, config);
        }

        private bool IsPlayer(Actor actor_) => actor_ != null && actor_ == _state.Player;
    }
}
