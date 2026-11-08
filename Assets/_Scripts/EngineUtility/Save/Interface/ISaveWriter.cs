namespace _DreamKeep.EngineUtility
{
    /// <summary>存档写袋：业务侧按名字写入自己的字段，不接触任何序列化类型。</summary>
    public interface ISaveWriter
    {
        void Write<T>(string key_, T value_);
    }
}
