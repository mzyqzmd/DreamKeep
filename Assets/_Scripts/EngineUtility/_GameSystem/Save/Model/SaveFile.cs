using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace _DreamKeep.EngineUtility
{
    /// <summary>单文件存档根：以实现类类型全名为键的数据袋集合，只做数据容器不放逻辑。
    /// 不带版本号：键即类型全名，字段增删由读写袋的缺省值回落兜住，语义变更由人工清档处理。</summary>
    internal sealed class SaveFile
    {
        public Dictionary<string, JObject> Entries { get; set; } = new();
    }
}
