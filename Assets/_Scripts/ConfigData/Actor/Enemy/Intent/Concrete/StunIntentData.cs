using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>击晕意图配置：无参数。击晕通常由位移撞击在运行时强行覆盖上来，
    /// 保留本类型是为了让"击晕"与其他意图一样可被配置与烘焙。</summary>
    [Serializable]
    [AddTypeMenu("Intent/击晕")]
    public sealed class StunIntentData : IntentData
    {
    }
}
