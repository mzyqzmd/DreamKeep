using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>2D 通用音频播放器：固定槽位池 + 优先级抢占 + 双渐变。槽位即池——Awake 建满 AudioSource，
    /// 播放占用、到期自动腾出；槽满时抢「优先级最低且开播最早」的槽，同优先级不互抢、抢不到静默丢弃。
    /// 只服务 2D 音频（spatialBlend 恒 1），不认识任何业务类型；注册与注入由 Presentation 侧组合根承担。</summary>
    public sealed class AudioRoot : MonoBehaviour
    {
        [Tooltip("并发槽位数：2D 卡牌游戏 12 绰绰有余")]
        [SerializeField] private int _slotCount = 12;

        private AudioVoice[] _voices;

        private void Awake()
        {
            _voices = new AudioVoice[Mathf.Max(1, _slotCount)];
            for (int i = 0; i < _voices.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f; // 纯 2D：位置无语义
                _voices[i].Source = source;
            }
        }

        private void Update()
        {
            float now = Time.time;
            for (int i = 0; i < _voices.Length; i++)
                TickVoice(ref _voices[i], now);
        }

        #region 对外 API：播放 / 停止全部 / 暂停全部 / 恢复全部

        /// <summary>播放一条 cue：clip 变体、音调与音量由 cue 自己定；槽满时抢优先级最低且开播最早的槽，抢不到静默丢弃。</summary>
        public void Play(SO_AudioCue cue_)
        {
            AudioClip clip = cue_.GetClip();
            if (!clip) return;
            if (!TryAcquireSlot(cue_.Priority, out int index)) return;
            float pitch = cue_.GetPitch();
            float volume = cue_.GetVolume(); // 本次播放唯一采样：淡入终点、常态音量、恢复淡回共用

            ref AudioVoice voice = ref _voices[index];
            AudioSource source = voice.Source;
            source.outputAudioMixerGroup = cue_.MixerGroup;
            source.clip = clip;
            source.pitch = pitch;
            source.loop = cue_.Loop;
            source.volume = 0f;
            source.Play();

            voice.Occupied = true;
            voice.Cue = cue_;
            voice.Priority = cue_.Priority;
            voice.StartTime = Time.time;
            voice.EndTime = cue_.Loop ? float.PositiveInfinity : Time.time + clip.length / pitch;
            voice.TargetVolume = volume;
            voice.IsPaused = false;
            voice.PendingPause = false;
            voice.PendingStop = false;

            // 淡入：0 → 目标音量；时长 0 直接硬进
            float fadeIn = cue_.FadeInSeconds;
            if (fadeIn <= 0f)
            {
                source.volume = volume;
                voice.FadeRemaining = 0f;
                return;
            }

            voice.FadeTo = volume;
            voice.FadeSpeed = volume / fadeIn;
            voice.FadeRemaining = fadeIn;
        }

        /// <summary>停止全部声源：活动的逐条淡出后释放，冻结中的（音量已在 0）直接释放；清场用。</summary>
        public void StopAll()
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                ref AudioVoice voice = ref _voices[i];
                if (!voice.Occupied || voice.PendingStop) continue;
                if (voice.IsPaused || voice.Cue.FadeOutSeconds <= 0f)
                {
                    ReleaseVoice(ref voice);
                    continue;
                }

                voice.FadeTo = 0f;
                voice.FadeSpeed = voice.Source.volume / voice.Cue.FadeOutSeconds; // 当前音量 → 0 匀速走完
                voice.FadeRemaining = voice.Cue.FadeOutSeconds;
                voice.PendingStop = true;
                voice.PendingPause = false;
            }
        }

        /// <summary>暂停全部活动声源：逐条淡出后冻结；只作用按下那一刻已活动的声源，此后新 Play 照常响。</summary>
        public void PauseAll()
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                ref AudioVoice voice = ref _voices[i];
                if (!voice.Occupied || voice.IsPaused || voice.PendingStop) continue;
                if (voice.Cue.FadeOutSeconds <= 0f)
                {
                    voice.Source.Pause();
                    voice.IsPaused = true;
                    continue;
                }

                voice.FadeTo = 0f;
                voice.FadeSpeed = voice.Source.volume / voice.Cue.FadeOutSeconds;
                voice.FadeRemaining = voice.Cue.FadeOutSeconds;
                voice.PendingPause = true;
            }
        }

        /// <summary>恢复全部冻结声源：解冻后从 0 淡回目标音量；暂停期间已到期的按自然消亡释放，不复活。</summary>
        public void ResumeAll()
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                ref AudioVoice voice = ref _voices[i];
                if (!voice.Occupied || !voice.IsPaused) continue;
                if (voice.EndTime <= Time.time)
                {
                    ReleaseVoice(ref voice);
                    continue;
                }

                voice.Source.UnPause();
                voice.IsPaused = false;
                float fadeIn = voice.Cue.FadeInSeconds;
                if (fadeIn <= 0f)
                {
                    voice.Source.volume = voice.TargetVolume;
                    continue;
                }

                voice.Source.volume = 0f;
                voice.FadeTo = voice.TargetVolume;
                voice.FadeSpeed = voice.TargetVolume / fadeIn;
                voice.FadeRemaining = fadeIn;
                voice.PendingPause = false;
            }
        }

        #endregion

        // 单遍扫一个槽：先确定性到期释放（淡出没走完也直接收），再推进渐变，淡完按去向落锤
        private void TickVoice(ref AudioVoice voice_, float now_)
        {
            if (!voice_.Occupied || voice_.IsPaused) return;
            float dt = Time.deltaTime;

            if (voice_.EndTime <= now_)
            {
                ReleaseVoice(ref voice_);
                return;
            }

            if (voice_.FadeRemaining <= 0f) return;
            voice_.FadeRemaining -= dt;
            if (voice_.FadeRemaining > 0f)
            {
                voice_.Source.volume = Mathf.MoveTowards(voice_.Source.volume, voice_.FadeTo, voice_.FadeSpeed * dt);
                return;
            }

            voice_.Source.volume = voice_.FadeTo;
            voice_.FadeRemaining = 0f;
            if (voice_.PendingPause)
            {
                voice_.Source.Pause();
                voice_.IsPaused = true;
                voice_.PendingPause = false;
            }
            else if (voice_.PendingStop)
            {
                ReleaseVoice(ref voice_);
            }
        }

        // 取槽：先找空槽；全满才扫「优先级更低」里开播最早的（同优先级不互抢），抢到即硬停旧声源，抢不到丢弃
        private bool TryAcquireSlot(int priority_, out int index_)
        {
            index_ = -1;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (_voices[i].Occupied) continue;
                index_ = i;
                return true;
            }

            int victim = -1;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (_voices[i].Priority >= priority_) continue;
                if (victim < 0 || _voices[i].StartTime < _voices[victim].StartTime) victim = i;
            }

            if (victim < 0) return false;
            index_ = victim;
            ReleaseVoice(ref _voices[victim]); // 被抢即硬停：它已在让路，二次淡出无意义
            _voices[victim].Occupied = true;   // 槽马上转交给新声源
            return true;
        }

        // 释放槽位：停声源、清占用与渐变态；声源组件常驻复用，不销毁
        private void ReleaseVoice(ref AudioVoice voice_)
        {
            voice_.Source.Stop();
            voice_.Source.clip = null;
            voice_.Occupied = false;
            voice_.IsPaused = false;
            voice_.PendingPause = false;
            voice_.PendingStop = false;
            voice_.FadeRemaining = 0f;
        }
    }
}
