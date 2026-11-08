using Newtonsoft.Json.Linq;

namespace _DreamKeep.EngineUtility
{
    /// <summary>存档写袋：把具名值转成 JToken 收进 JObject，供门面聚合成单文件。</summary>
    internal sealed class SaveWriter : ISaveWriter
    {
        private readonly JObject _bag = new();

        public JObject Bag => _bag;

        #region 写入具名值：空引用落 JSON null，其余按实际类型转换

        public void Write<T>(string key_, T value_)
        {
            object boxed = value_;
            _bag[key_] = boxed == null ? JValue.CreateNull() : JToken.FromObject(value_);
        }

        #endregion
    }
}
