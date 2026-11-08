namespace _DreamKeep.EngineUtility
{
    /// <summary>存档读袋：按名字读回字段，缺项或类型不符时返回调用方给的缺省值。</summary>
    public interface ISaveReader
    {
        T Read<T>(string key_, T fallback_);
    }
}
