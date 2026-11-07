using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>音频槽位状态：仅数据无逻辑，由 AudioRoot 统一驱动——到期释放、渐变推进与去向执行都靠这些字段判定。</summary>
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
    }
}
