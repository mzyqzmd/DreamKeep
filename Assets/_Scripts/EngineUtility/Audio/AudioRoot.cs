using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace _DreamKeep.EngineUtility
{
    /// <summary>2D 通用音频播放器：两个子节点分别承载 BGM 与 SFX 的 AudioSource，本组件只负责驱动、不承载声源。
    /// SFX 走固定槽位池 + 优先级抢占，BGM 走两条独立声源交叉渐变。
    /// 只服务 2D 音频（spatialBlend 恒 1），不认识任何业务类型；注册与注入由 Presentation 侧组合根承担。
    /// 频道音量由表现层设置面板写入两个静态系数，本类按平方律换算成 dB 写进各自 mixer 的暴露参数，挂该组的声源即时生效。</summary>
    public sealed class AudioRoot : MonoBehaviour
    {
        [Tooltip("并发槽位数：2D 卡牌游戏 12 绰绰有余")] [SerializeField]
        private int _slotCount = 12;

        [Tooltip("BGM 源节点：本节点上的两条 AudioSource 由本类驱动，用于切歌交叉渐变")] [SerializeField]
        private GameObject _bgmRoot;

        [Tooltip("SFX 源节点：并发槽位的 AudioSource 全挂在这里，AudioRoot 自身不承载声源")] [SerializeField]
        private GameObject _sfxRoot;

        [Tooltip("集中引用库：所有 cue 只在这份资产里接一次")] [SerializeField]
        private SO_AudioCueLibrary _cues;

        [Tooltip("BGM 混音器：频道音量写它的暴露参数 Volume")] [SerializeField]
        private AudioMixer _bgmMixer;

        [Tooltip("音效混音器：频道音量写它的暴露参数 Volume")] [SerializeField]
        private AudioMixer _sfxMixer;

        /// <summary>BGM 频道系数（0~1）：表现层设置面板写入，本类不认识设置服务。</summary>
        public static float BgmVolume { get; set; } = 1f;

        /// <summary>音效频道系数（0~1）：同上。</summary>
        public static float SfxVolume { get; set; } = 1f;

        /// <summary>集中引用出口：消费点读 Cues 上的具名属性取 cue，本类不解释语义。</summary>
        public SO_AudioCueLibrary Cues => _cues;

        private const string VolumeParameter = "Volume";
        private const float MinDecibel = -80f;

        private AudioVoice[] _voices;
        private AudioBgmVoice[] _bgmVoices;
        private int _bgmActive = -1;
        private float _appliedBgm = -1f; // 已写进 mixer 的系数，初值 -1 保证首次必写
        private float _appliedSfx = -1f;

        private void Awake()
        {
            AudioSource[] sfxSources = EnsureSources(_sfxRoot, Mathf.Max(1, _slotCount));
            _voices = new AudioVoice[sfxSources.Length];
            for (int i = 0; i < _voices.Length; i++) _voices[i].Source = sfxSources[i];

            AudioSource[] bgmSources = EnsureSources(_bgmRoot, 2);
            _bgmVoices = new AudioBgmVoice[2];
            _bgmVoices[0].Source = bgmSources[0];
            _bgmVoices[1].Source = bgmSources[1];

            ApplyChannelVolumes(); // 先落一次音量，避免按 mixer 默认 0dB 出声
        }

        // 两个节点统一形态：声源只挂在各自节点上、不足才补，AudioRoot 自身不承载任何 AudioSource
        private static AudioSource[] EnsureSources(GameObject root_, int count_)
        {
            var sources = new List<AudioSource>(root_.GetComponents<AudioSource>());
            while (sources.Count < count_) sources.Add(root_.AddComponent<AudioSource>());
            AudioSource[] result = new AudioSource[count_];
            for (int i = 0; i < count_; i++)
            {
                AudioSource source = sources[i];
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 纯 2D：0 = 不做距离衰减与多普勒，声源位置无语义
                result[i] = source;
            }

            return result;
        }

        private void Update()
        {
            ApplyChannelVolumes(); // 滑条改动经静态系数进来，差量写 mixer
            float now = Time.time;
            for (int i = 0; i < _voices.Length; i++)
                TickVoice(ref _voices[i], now);
            TickBgm();
        }

        #region 对外 API：播放 / BGM 切歌 / 停止全部 / 暂停全部 / 恢复全部

        /// <summary>播放一条 cue：clip 变体、音调与音量由 cue 自己定；槽满时抢优先级最低且开播最早的槽，抢不到静默丢弃。</summary>
        public void Play(SO_AudioCue cue_)
        {
            if (_voices == null) return; // Awake 未跑：父节点 Awake 里就可能来播首帧音效
            if (!cue_) return;
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

            voice.BeginFadeIn(volume, cue_.FadeInSeconds); // 0 → 目标音量；时长 0 直接硬进
        }

        /// <summary>切换 BGM：新曲走另一条源淡入、旧曲淡出后停止；连续快切时直接硬停正在让路的源。</summary>
        public void PlayBgm(SO_AudioCue cue_)
        {
            if (_bgmVoices == null) return; // Awake 未跑：主菜单父节点 Awake 里就会点首曲
            if (!cue_) return;
            AudioClip clip = cue_.GetClip();
            if (!clip) return;
            // 同一首曲子重复请求不重启：CompleteSegment 等幂等路径会反复要求回到跑图曲
            if (_bgmActive >= 0 && _bgmVoices[_bgmActive].Occupied
                                && _bgmVoices[_bgmActive].Cue == cue_ && !_bgmVoices[_bgmActive].PendingStop) return;

            int index = _bgmActive < 0 ? 0 : 1 - _bgmActive;
            if (_bgmVoices[index].Occupied) ReleaseBgmVoice(ref _bgmVoices[index]);
            if (_bgmActive >= 0) BeginBgmFadeOut(_bgmActive);

            ref AudioBgmVoice voice = ref _bgmVoices[index];
            AudioSource source = voice.Source;
            source.outputAudioMixerGroup = cue_.MixerGroup;
            source.clip = clip;
            source.pitch = cue_.GetPitch();
            source.loop = cue_.Loop;
            source.volume = 0f;
            source.Play();

            voice.Occupied = true;
            voice.Cue = cue_;
            voice.TargetVolume = cue_.GetVolume();
            voice.PendingStop = false;

            voice.BeginFadeIn(voice.TargetVolume, cue_.FadeInSeconds);
            _bgmActive = index;
        }

        /// <summary>停止 BGM：两条源各自淡出后释放；不触碰 12 个 SFX 槽。</summary>
        public void StopBgm()
        {
            if (_bgmVoices == null) return;
            for (int i = 0; i < _bgmVoices.Length; i++)
                BeginBgmFadeOut(i);
            _bgmActive = -1;
        }

        /// <summary>停止全部声源：活动的逐条淡出后释放，冻结中的（音量已在 0）直接释放；清场用。</summary>
        public void StopAll()
        {
            if (_voices == null) return;
            for (int i = 0; i < _voices.Length; i++)
            {
                ref AudioVoice voice = ref _voices[i];
                if (!voice.Occupied || voice.PendingStop) continue;
                if (voice.IsPaused || voice.Cue.FadeOutSeconds <= 0f)
                {
                    ReleaseVoice(ref voice);
                    continue;
                }

                voice.BeginFadeOut(voice.Cue.FadeOutSeconds); // 当前音量 → 0 匀速走完
                voice.PendingStop = true;
                voice.PendingPause = false;
            }
        }

        /// <summary>暂停全部活动声源：逐条淡出后冻结；只作用按下那一刻已活动的声源，此后新 Play 照常响。</summary>
        public void PauseAll()
        {
            if (_voices == null) return;
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

                voice.BeginFadeOut(voice.Cue.FadeOutSeconds);
                voice.PendingPause = true;
            }
        }

        /// <summary>恢复全部冻结声源：解冻后从 0 淡回目标音量；暂停期间已到期的按自然消亡释放，不复活。</summary>
        public void ResumeAll()
        {
            if (_voices == null) return;
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
                voice.Source.volume = 0f;
                voice.BeginFadeIn(voice.TargetVolume, voice.Cue.FadeInSeconds); // 时长 0 时直接落到目标音量
                voice.PendingPause = false;
            }
        }

        #endregion

        // 频道音量：系数变了才写一次 mixer，避免每帧 SetFloat
        private void ApplyChannelVolumes()
        {
            if (!Mathf.Approximately(_appliedBgm, BgmVolume))
            {
                _appliedBgm = BgmVolume;
                _bgmMixer.SetFloat(VolumeParameter, ToDecibel(BgmVolume));
            }

            if (!Mathf.Approximately(_appliedSfx, SfxVolume))
            {
                _appliedSfx = SfxVolume;
                _sfxMixer.SetFloat(VolumeParameter, ToDecibel(SfxVolume));
            }
        }

        // 平方律换算：滑条 0.5 落 -12dB、0.25 落 -24dB，听感近似等步进；0.0001 以下直接静音
        private static float ToDecibel(float volume_)
        {
            if (volume_ <= 0.0001f) return MinDecibel;
            return Mathf.Clamp(40f * Mathf.Log10(volume_), MinDecibel, 0f);
        }

        // 让指定 BGM 源淡出：已在让路的源不重开渐变，淡出时长 0 直接释放
        private void BeginBgmFadeOut(int index_)
        {
            ref AudioBgmVoice voice = ref _bgmVoices[index_];
            if (!voice.Occupied || voice.PendingStop) return;
            float fade = voice.Cue.FadeOutSeconds;
            if (fade <= 0f)
            {
                ReleaseBgmVoice(ref voice);
                return;
            }

            voice.BeginFadeOut(fade);
            voice.PendingStop = true;
        }

        // 单遍扫两条 BGM 源：推进渐变、淡完按去向落锤；频道音量归 mixer，不在此处乘
        private void TickBgm()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _bgmVoices.Length; i++)
            {
                ref AudioBgmVoice voice = ref _bgmVoices[i];
                if (!voice.Occupied) continue;

                if (voice.FadeRemaining <= 0f)
                {
                    if (voice.PendingStop) ReleaseBgmVoice(ref voice);
                    else voice.Source.volume = voice.TargetVolume;
                    continue;
                }

                voice.FadeRemaining -= dt;
                if (voice.FadeRemaining > 0f)
                {
                    voice.Source.volume = Mathf.MoveTowards(voice.Source.volume, voice.FadeTo, voice.FadeSpeed * dt);
                    continue;
                }

                voice.Source.volume = voice.FadeTo;
                voice.FadeRemaining = 0f;
                if (voice.PendingStop) ReleaseBgmVoice(ref voice);
            }
        }

        private void ReleaseBgmVoice(ref AudioBgmVoice voice_)
        {
            voice_.Source.Stop();
            voice_.Source.clip = null;
            voice_.Occupied = false;
            voice_.PendingStop = false;
            voice_.FadeRemaining = 0f;
        }

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
            _voices[victim].Occupied = true; // 槽马上转交给新声源
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
