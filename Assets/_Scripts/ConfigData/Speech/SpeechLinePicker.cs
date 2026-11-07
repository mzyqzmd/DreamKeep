using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>台词随机取词：空表返回 null，调用方据此早退；台词是纯表现随机，不占用整局随机流。</summary>
    public static class SpeechLinePicker
    {
        #region 从候选行里随机取一条

        public static string Pick(IReadOnlyList<string> lines_) =>
            lines_ == null || lines_.Count == 0 ? null : lines_[Random.Range(0, lines_.Count)];

        #endregion
    }
}
