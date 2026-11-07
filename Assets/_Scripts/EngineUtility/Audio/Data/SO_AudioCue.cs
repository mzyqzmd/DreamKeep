using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Audio;

namespace _DreamKeep.EngineUtility
{
    /// <summary>音频配置资产：一声音一个 cue——clip 变体、音调与音量各在范围内随机取值，
    /// 音量分级归 AudioMixer 管。淡入淡出默认 0.5 秒渐变，填 0 即硬进硬出。</summary>
    [CreateAssetMenu(fileName = "AudioCue", menuName = "DreamKeep/Audio/AudioCue")]
    public sealed class SO_AudioCue : ScriptableObject
    {
        [Tooltip("同义变体列表：每次播放随机抽一条，空列表或空引用跳过本次播放")] [SerializeField]
        private List<AudioClip> _clips = new();

        [Tooltip("可空：播放时落到声源的 outputAudioMixerGroup，音量分级归 mixer 管")] [SerializeField]
        private AudioMixerGroup _mixerGroup;

        [Tooltip("目标音量范围：x 下限、y 上限，双滑块保证有序且都在 0~1")] [MinMaxSlider(0f, 1f)] [SerializeField]
        private Vector2 _volumeRange = new(1f, 1f);

        [Tooltip("音调随机范围：x 下限、y 上限，双滑块保证有序且都在 0.1~2")] [MinMaxSlider(0.1f, 2f)] [SerializeField]
        private Vector2 _pitchRange = new(0.9f, 1.1f);

        [Tooltip("越大越优先：槽满时抢占「优先级最低且开播最早」的槽，同优先级不互抢")] [SerializeField]
        private int _priority;

        [SerializeField] private bool _loop;

        [Tooltip("播放音量 0→目标 的渐变时长，0 = 硬进")] [Min(0f)] [SerializeField]
        private float _fadeInSeconds = 0.5f;

        [Tooltip("停止/暂停音量 当前→0 的渐变时长，0 = 硬出")] [Min(0f)] [SerializeField]
        private float _fadeOutSeconds = 0.5f;

        public AudioMixerGroup MixerGroup => _mixerGroup;
        public int Priority => _priority;
        public bool Loop => _loop;
        public float FadeInSeconds => _fadeInSeconds;
        public float FadeOutSeconds => _fadeOutSeconds;

        /// <summary>随机抽本次播放的 clip 变体：列表为空返回 null，由调用方跳过播放。</summary>
        public AudioClip GetClip() => _clips.Count == 0 ? null : _clips[Random.Range(0, _clips.Count)];

        /// <summary>随机取本次播放的目标音量：一次采样贯穿整条声源生命周期，避免渐变终点跳变。</summary>
        public float GetVolume() => Random.Range(_volumeRange.x, _volumeRange.y);

        /// <summary>随机取本次播放的音调：范围有序由 MinMaxSlider 在 Inspector 保证，随机流归 cue 自己管。</summary>
        public float GetPitch() => Random.Range(_pitchRange.x, _pitchRange.y);
    }
}