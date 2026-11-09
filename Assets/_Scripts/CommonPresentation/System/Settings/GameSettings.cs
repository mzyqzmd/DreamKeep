using System;
using System.Collections.Generic;
using UnityEngine;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>应用级偏好真源（BGM/音效音量、分辨率、全屏）：住通用表现层、与单局存档无关，
    /// 主菜单与整局共用同一份；每次写入即落 PlayerPrefs，启动时由常驻编排者调一次 ApplyOnBoot 恢复。
    /// 音量只写 AudioRoot 的静态系数（工具层不认识本类），分辨率与全屏直接走 Screen。</summary>
    public static class GameSettings
    {
        private const string BgmKey = "DreamKeep.Setting.BgmVolume";
        private const string SfxKey = "DreamKeep.Setting.SfxVolume";
        private const string FullscreenKey = "DreamKeep.Setting.Fullscreen";
        private const string WidthKey = "DreamKeep.Setting.ResolutionWidth";
        private const string HeightKey = "DreamKeep.Setting.ResolutionHeight";

        /// <summary>下拉首项「自动」的索引：表示跟随显示器当前分辨率。</summary>
        public const int AutoResolutionIndex = 0;

        // 固定常用档位（上限 2560 × 1440）：不枚举 Screen.resolutions，任意显示器下选项一致
        private static readonly Vector2Int[] _presetResolutions =
        {
            new(1280, 720),
            new(1366, 768),
            new(1600, 900),
            new(1920, 1080),
            new(2560, 1440)
        };

        private static readonly List<string> _labels = new();
        private static int _resolutionIndex;

        public static float BgmVolume { get; private set; } = 1f;
        public static float SfxVolume { get; private set; } = 1f;
        public static bool Fullscreen { get; private set; } = true;
        public static int ResolutionIndex => _resolutionIndex;

        /// <summary>下拉选项表：索引 0 恒为「自动」，其后为固定常用档位。</summary>
        public static IReadOnlyList<string> ResolutionLabels => _labels;

        static GameSettings()
        {
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, 1f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
            _labels.Add("自动");
            foreach (Vector2Int size in _presetResolutions)
                _labels.Add($"{size.x} × {size.y}");
            _resolutionIndex = FindSavedIndex(PlayerPrefs.GetInt(WidthKey, 0), PlayerPrefs.GetInt(HeightKey, 0));
        }

        // 落盘的是宽高而非索引：档位表调整也不会选错；IndexOf 未命中为 -1，+1 后正好回到「自动」
        private static int FindSavedIndex(int width_, int height_) =>
            width_ <= 0 || height_ <= 0
                ? AutoResolutionIndex
                : Array.IndexOf(_presetResolutions, new Vector2Int(width_, height_)) + 1;

        #region 对外：写入即生效并落盘

        public static void SetBgmVolume(float value_)
        {
            BgmVolume = Mathf.Clamp01(value_);
            AudioRoot.BgmVolume = BgmVolume;
            PlayerPrefs.SetFloat(BgmKey, BgmVolume);
        }

        public static void SetSfxVolume(float value_)
        {
            SfxVolume = Mathf.Clamp01(value_);
            AudioRoot.SfxVolume = SfxVolume;
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        }

        public static void SetFullscreen(bool value_)
        {
            Fullscreen = value_;
            Screen.fullScreen = value_;
            PlayerPrefs.SetInt(FullscreenKey, value_ ? 1 : 0);
        }

        /// <summary>按下拉索引应用分辨率：0 = 跟随显示器，其余取固定档位；越界忽略。</summary>
        public static void SetResolutionIndex(int index_)
        {
            if (index_ < 0 || index_ > _presetResolutions.Length) return;
            _resolutionIndex = index_;
            Vector2Int size = SizeOf(index_);
            PlayerPrefs.SetInt(WidthKey, size.x);
            PlayerPrefs.SetInt(HeightKey, size.y);
            ApplyResolution(size);
        }

        /// <summary>启动恢复：音量系数写回工具层、分辨率与全屏按落盘值应用一次（常驻编排者 Awake 调）。</summary>
        public static void ApplyOnBoot()
        {
            AudioRoot.BgmVolume = BgmVolume;
            AudioRoot.SfxVolume = SfxVolume;
            ApplyResolution(SizeOf(_resolutionIndex));
        }

        #endregion

        // 索引 0 是「自动」（零尺寸），其余对应档位表前一位
        private static Vector2Int SizeOf(int index_) =>
            index_ == AutoResolutionIndex ? Vector2Int.zero : _presetResolutions[index_ - 1];

        private static void ApplyResolution(Vector2Int size_)
        {
            Screen.fullScreen = Fullscreen;
#if UNITY_EDITOR
            // 编辑器下 Game 视图分辨率由 Game 视图自己决定，SetResolution 只会打乱调试画面：只落盘不应用
#else
            if (size_.x <= 0 || size_.y <= 0)
            {
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, Fullscreen);
                return;
            }
            Screen.SetResolution(size_.x, size_.y, Fullscreen);
#endif
        }
    }
}