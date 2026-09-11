using Cysharp.Threading.Tasks;
using Spine.Unity;
using _DreamKeep.Data;
using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>玩家视图：在角色视图基类之上补玩家独有的施法、举盾、睡姿与开场动作，并按卡面决定出牌播什么。</summary>
    public class Obj_PlayerView : Obj_ActorView
    {
        [SerializeField] private AnimationReferenceAsset _skill;
        [SerializeField] private AnimationReferenceAsset _parry;
        [SerializeField] private AnimationReferenceAsset _parryStand;
        [SerializeField] private AnimationReferenceAsset _sleep;
        [SerializeField] private AnimationReferenceAsset _wake;

        private bool _isBlocking;

        /// <summary>是否停在举盾待机，播放队列据此跳过重复的举盾起手。</summary>
        public bool IsBlocking => _isBlocking;

        #region 开场：播一次醒来动作后回到待机
        public UniTask PlayWake()
        {
            _isBlocking = false;
            return PlayOneShot(_wake);
        }
        #endregion

        #region 出牌：按卡面类型与词条选动作，举盾类直接进举盾待机
        public UniTask PlayCard(CardModel card_, SO_Card config_)
        {
            AnimationReferenceAsset anim = ResolveActAnimation(card_, config_);
            if (anim == _parry) return PlayBlock();
            _isBlocking = false;
            return PlayOneShot(anim);
        }
        #endregion

        #region 受击：播一次受伤动作，举盾待机被打断
        public UniTask PlayHurt()
        {
            _isBlocking = false;
            return PlayOneShot(_hurt);
        }
        #endregion

        #region 举盾：起手后转入举盾待机，已在举盾时跳过起手
        public UniTask PlayBlock()
        {
            if (_isBlocking) return UniTask.CompletedTask;
            _isBlocking = true;
            return PlayOneShotThenLoop(_parry, _parryStand);
        }
        #endregion

        #region 落盾：举盾待机退回普通待机
        public void PlayLeaveBlock()
        {
            if (!_isBlocking) return;
            _isBlocking = false;
            PlayLoop(_stand);
        }
        #endregion

        // 收场：转入睡姿循环
        public void PlaySleep() => PlayLoop(_sleep);

        // 动作选择：攻击或含伤害取主动作，其次含格挡取举盾，其余取施法，配置缺失时兜底施法
        private AnimationReferenceAsset ResolveActAnimation(CardModel card_, SO_Card config_)
        {
            if (config_ == null) return _skill;
            if (config_.Type == ECardType.Attack || HasAffix<CA_Damage>(card_)) return _act;
            if (HasAffix<CA_Block>(card_)) return _parry;
            return _skill;
        }

        private static bool HasAffix<T>(CardModel card_) where T : CardAffix
        {
            for (int i = 0; i < card_.ActiveAffixes.Count; i++)
                if (card_.ActiveAffixes[i] is T) return true;
            return false;
        }
    }
}
