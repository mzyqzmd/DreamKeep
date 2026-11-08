using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>音频槽位状态：渐变起停由本结构自理（BeginFadeIn/BeginFadeOut），
    /// 到期释放、渐变推进与去向执行仍由 AudioRoot 统一驱动。</summary>
    internal struct AudioVoice
    {
        public AudioSource Source;
        public bool Occupied;        // 槽占用标记：取出即置真，到期/停止释放即置假
        public SO_AudioCue Cue;      // 出生配置：暂停/恢复的渐变时长按它取
        public int Priority;
        public float StartTime;      // 开播时刻：抢占时比「谁最老」用
        public float EndTime;        // 确定性到期：开播时刻 + clip.length / pitch；loop 恒正无穷
        public float TargetVolume;   // 本次播放采样的目标音量：淡入与恢复淡回的终点
        public float FadeTo;         // 当前渐变终点音量
        public float FadeSpeed;      // 渐变速率（正数，MoveTowards 用）
        public float FadeRemaining;  // 剩余渐变秒数，0 = 无渐变进行中
        public bool PendingPause;    // 渐变结束后冻结声源
        public bool PendingStop;     // 渐变结束后停止并释放槽
        public bool IsPaused;        // 冻结中（ResumeAll 的作用集）

        /// <summary>起点写 0 后开始淡入：速率按目标音量算，时长非正直接落到目标、不留渐变。
        /// 调用前声源必须已归零，保证从静音起播。</summary>
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

        /// <summary>开始淡出到静音：速率按当前音量算（暂停/停止用的是按下那一刻的音量），
        /// 淡完去向由调用方的 PendingStop / PendingPause 决定。</summary>
        public void BeginFadeOut(float seconds_)
        {
            FadeTo = 0f;
            FadeSpeed = Source.volume / seconds_;
            FadeRemaining = seconds_;
        }
    }
}
