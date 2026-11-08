using System;
using Newtonsoft.Json.Linq;

namespace _DreamKeep.EngineUtility
{
    /// <summary>存档读袋：从 JObject 读回具名值，缺项、类型不符或解析异常都退到缺省值，不扩散成整份档失败。</summary>
    internal sealed class SaveReader : ISaveReader
    {
        private readonly JObject _bag;

        public SaveReader(JObject bag_) => _bag = bag_;

        #region 读取具名值：任何失败都返回调用方给的缺省值

        public T Read<T>(string key_, T fallback_)
        {
            JToken token = _bag?[key_];
            if (token == null || token.Type == JTokenType.Null) return fallback_;
            try { return token.ToObject<T>(); }
            catch (Exception) { return fallback_; }
        }

        #endregion
    }
}
