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

        private static readonly List<Vector2Int> _resolutions = new();
        private static readonly List<string> _labels = new();
        private static int _resolutionIndex;

        public static float BgmVolume { get; private set; } = 1f;
        public static float SfxVolume { get; private set; } = 1f;
        public static bool Fullscreen { get; private set; } = true;
        public static int ResolutionIndex => _resolutionIndex;

        /// <summary>下拉选项表：索引 0 恒为「自动」，其后是去重后的屏幕分辨率；每次读取按当前屏幕重建。</summary>
        public static IReadOnlyList<string> ResolutionLabels
        {
            get
            {
                BuildResolutionOptions();
                return _labels;
            }
        }

        static GameSettings()
        {
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, 1f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
            BuildResolutionOptions();
        }

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

        /// <summary>按下拉索引应用分辨率：0 = 跟随显示器，其余取去重表里的宽高；越界忽略。</summary>
        public static void SetResolutionIndex(int index_)
        {
            if (index_ < 0 || index_ >= _resolutions.Count) return;
            _resolutionIndex = index_;
            Vector2Int size = _resolutions[index_];
            PlayerPrefs.SetInt(WidthKey, size.x);
            PlayerPrefs.SetInt(HeightKey, size.y);
            ApplyResolution(size);
        }

        /// <summary>启动恢复：音量系数写回工具层、分辨率与全屏按落盘值应用一次（常驻编排者 Awake 调）。</summary>
        public static void ApplyOnBoot()
        {
            AudioRoot.BgmVolume = BgmVolume;
            AudioRoot.SfxVolume = SfxVolume;
            ApplyResolution(_resolutions[_resolutionIndex]);
        }

        #endregion

        // 选项表按当前屏幕重建：首项「自动」占索引 0，分辨率按宽高去重（同宽高不同刷新率只留一条）
        private static void BuildResolutionOptions()
        {
            _resolutions.Clear();
            _labels.Clear();
            _resolutions.Add(Vector2Int.zero);
            _labels.Add("自动");
            for (int i = Screen.resolutions.Length - 1; i >= 0; i--)
            {
                Vector2Int size = new(Screen.resolutions[i].width, Screen.resolutions[i].height);
                if (_resolutions.Contains(size)) continue;
                _resolutions.Add(size);
                _labels.Add($"{Screen.resolutions[i].width} × {Screen.resolutions[i].height}");
            }

            _resolutionIndex = FindSavedIndex(PlayerPrefs.GetInt(WidthKey, 0), PlayerPrefs.GetInt(HeightKey, 0));
        }

        // 落盘的是宽高而非索引：分辨率表随显示器变化，按宽高反查不会选错
        private static int FindSavedIndex(int width_, int height_)
        {
            if (width_ <= 0 || height_ <= 0) return AutoResolutionIndex;
            int index = _resolutions.IndexOf(new Vector2Int(width_, height_));
            return index < 0 ? AutoResolutionIndex : index;
        }

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