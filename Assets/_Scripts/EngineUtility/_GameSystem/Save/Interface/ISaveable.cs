namespace _DreamKeep.EngineUtility
{
    /// <summary>存档契约：任何类实现本接口即可把自己的数据纳入唯一那份本地存档，无需改动存档框架。
    /// 数据袋的键由框架按实现类类型全名生成，同一类型同时只应有一个实例参与存档。</summary>
    public interface ISaveable
    {
        void Capture(ISaveWriter writer_);
        void Restore(ISaveReader reader_);
    }
}
