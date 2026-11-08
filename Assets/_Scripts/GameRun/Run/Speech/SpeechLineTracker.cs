using System.Collections.Generic;

namespace _DreamKeep.ConfigData
{
    /// <summary>台词次数台账：按「说话者 + 分组对象引用」计数，达上限返回 null 让调用方沉默；
    /// 实例级而非静态，随段 scope 销毁清零，不跨战斗残留。</summary>
    public sealed class SpeechLineTracker
    {
        private readonly Dictionary<(object, SpeechLineGroup), int> _played = new();

        #region 取词：未达上限才随机取一句并自增计数

        /// <summary>取一句台词：分组为空、空表或已达次数上限时返回 null；-1 表示不限次、不记台账。</summary>
        public string TryPick(object speaker_, SpeechLineGroup group_)
        {
            if (group_ == null) return null;
            if (group_.MaxPlays < 0) return group_.PickOneLine();

            (object, SpeechLineGroup) key = (speaker_, group_);
            _played.TryGetValue(key, out int count);
            if (count >= group_.MaxPlays) return null;

            string line = group_.PickOneLine();
            if (line != null) _played[key] = count + 1;
            return line;
        }

        /// <summary>台词没真正播出（被同侧不可打断的气泡挡掉、或找不到单位视图）时退回本次计数，避免永久沉默。</summary>
        public void Rollback(object speaker_, SpeechLineGroup group_)
        {
            if (group_ == null || group_.MaxPlays < 0) return;
            (object, SpeechLineGroup) key = (speaker_, group_);
            if (!_played.TryGetValue(key, out int count) || count <= 0) return;
            if (count == 1) _played.Remove(key);
            else _played[key] = count - 1;
        }

        #endregion
    }
}
