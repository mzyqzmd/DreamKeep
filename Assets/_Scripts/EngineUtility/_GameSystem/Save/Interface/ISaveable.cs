namespace _DreamKeep.EngineUtility
{
    /// <summary>存档契约：任何类实现本接口即可把自己的数据纳入唯一那份本地存档，无需改动存档框架。
    /// SaveKey 是数据袋的稳定键，与实现类类型名解耦，重命名类不会让旧档失联；同一键同时只应有一个实例参与存档。</summary>
    public interface ISaveable
    {
        string SaveKey { get; }
        void Capture(ISaveWriter writer_);
        void Restore(ISaveReader reader_);
    }
}
