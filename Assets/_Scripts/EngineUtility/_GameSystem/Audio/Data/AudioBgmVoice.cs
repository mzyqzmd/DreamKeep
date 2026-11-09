using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>BGM 单源状态：交叉渐变的起停由本结构自理（BeginFadeIn/BeginFadeOut），推进与释放仍由 AudioRoot 驱动；
    /// 频道音量归 mixer，不进本结构。与 AudioVoice 对称但不进抢占池——BGM 是常驻长循环，生命周期由切歌单独编排。</summary>
    internal struct AudioBgmVoice
    {
        public AudioSource Source;
        public SO_AudioCue Cue;
        public bool Occupied;
        public float TargetVolume; // 本次播放采样的 cue 音量，淡入终点与常态音量共用
        public float FadeTo;
        public float FadeSpeed;
        public float FadeRemaining;
        public bool PendingStop;

        /// <summary>起点写 0 后开始淡入：速率按目标音量算，时长非正直接落到目标、不留渐变。
        /// 目标音量本身由调用方先写进 TargetVolume（常态音量与淡入终点共用同一值）。</summary>
        public void BeginFadeIn(float target_, float seconds_)
        {
            FadeTo = target_;
            if (seconds_ <= 0f)
            {
                Source.volume = target_;
                FadeRemaining = 0f;
                return;
            }

            FadeSpeed = target_ / seconds_;
            FadeRemaining = seconds_;
        }

        /// <summary>开始淡出到静音：速率按当前音量算，淡完由调用方置 PendingStop 后释放。</summary>
        public void BeginFadeOut(float seconds_)
        {
            FadeTo = 0f;
            FadeSpeed = Source.volume / seconds_;
            FadeRemaining = seconds_;
        }
    }
}
