using System;
using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>一组台词：同类多条随机取一句，并自带播放次数上限（默认 1，-1 为不限次）。
    /// 分组对象本身即次数台账的键，按引用计数而非字符串键。</summary>
    [Serializable]
    public sealed class SpeechLineGroup
    {
        [SerializeField] private List<string> _lines = new();

        [Tooltip("最多播放次数，-1 为不限次")] [SerializeField]
        private int _maxPlays = 1;

        public string PickOneLine() => _lines == null || _lines.Count == 0
            ? null
            : _lines[UnityEngine.Random.Range(0, _lines.Count)];

        public int MaxPlays => _maxPlays;
    }
}