using UnityEngine;

namespace _DreamKeep.GameRun
{
    /// <summary>单条休息选项：标题、说明、图标、是否可点（false＝置灰，悬停文案仍显示）。</summary>
    public readonly struct RestOption
    {
        public string Title { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public bool Enabled { get; }

        public RestOption(string title_, string description_, Sprite icon_, bool enabled_)
        {
            Title = title_;
            Description = description_;
            Icon = icon_;
            Enabled = enabled_;
        }
    }
}
