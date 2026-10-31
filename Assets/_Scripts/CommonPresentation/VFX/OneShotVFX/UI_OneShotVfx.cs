using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>单次特效播放器：挂在特效预制体根上，自己识别身上是粒子系统还是帧动画，播一次并在总时长到点后回调；
    /// 宿主（VfxRoot）只负责取还池与摆位，不认识素材类型。画布元素额外做一次 alpha 兜底，避免静态帧在播前播后露脸。
    /// 子类可换掉"怎么播"与"播多久"（例如改由补间驱动），取池、摆位、到时还池的契约不变。</summary>
    public class UI_OneShotVfx : MonoBehaviour
    {
        [Tooltip("帧动画通道的片段：粒子通道留空")] [SerializeField]
        protected AnimationClip _clip;

        private ParticleSystem[] _particles;
        private Animator _animator;
        private Graphic _graphic;
        private Color _graphicColor;
        private int _playTicket; // 播放票号：实例复用后上一轮的延时回调据此作废

        protected virtual void Awake()
        {
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            _animator = GetComponent<Animator>();
            _graphic = GetComponent<Graphic>();
            if (!_graphic) return;
            _graphicColor = _graphic.color;
            WriteAlpha(0f);
        }

        #region 播放一次：开播 → 兜底 alpha → 到时回调还池

        public void Play(Action onFinished_)
        {
            int ticket = ++_playTicket;
            PlayOnce();
            WriteAlpha(1f);
            WaitAndFinishAsync(ticket, onFinished_).Forget();
        }

        #endregion

        #region 播放通道：默认是粒子与帧动画，子类可覆写换成别的驱动

        // 开播：粒子先清掉上一轮残留再整体开播（失活会硬切粒子）；帧动画从头直播片段，不走状态机转场以免被 Idle 抢回去
        protected virtual void PlayOnce()
        {
            if (_particles.Length > 0)
            {
                foreach (ParticleSystem particle in _particles) particle.Clear(true);
                foreach (ParticleSystem particle in _particles) particle.Play(true);
                return;
            }

            if (!_animator || !_clip) return;
            _animator.Play(_clip.name, 0, 0f);
            _animator.Update(0f);
        }

        // 本次总时长：粒子取各系统「发射时长 + 最长粒子寿命」的最大值，帧动画取片段长度
        protected virtual float Duration
        {
            get
            {
                if (_particles.Length == 0) return _clip ? _clip.length : 0f;
                float longest = 0f;
                foreach (ParticleSystem particle in _particles)
                {
                    ParticleSystem.MainModule main = particle.main;
                    longest = Mathf.Max(longest, main.duration + main.startLifetime.constantMax);
                }

                return longest;
            }
        }

        #endregion

        // 时长到点：票号不是最新就作废（说明已被下一次播放接棒），否则收起画布元素并回调
        private async UniTaskVoid WaitAndFinishAsync(int ticket_, Action onFinished_)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Duration), cancellationToken: destroyCancellationToken)
                .SuppressCancellationThrow();
            if (this == null || ticket_ != _playTicket) return;
            WriteAlpha(0f);
            onFinished_?.Invoke();
        }

        // 画布元素的可见性兜底：没有 Graphic 的纯粒子特效不受影响
        private void WriteAlpha(float alpha_)
        {
            if (!_graphic) return;
            Color color = _graphicColor;
            color.a = alpha_;
            _graphic.color = color;
        }
    }
}