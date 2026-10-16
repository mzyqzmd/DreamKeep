namespace _DreamKeep.RuntimeData
{
    /// <summary>描述文本里的数值渲染器：词条把基础值交给它，拿回该显示的文本片段（修正求值与着色都在实现方）。</summary>
    public interface IValueRenderer
    {
        string Render(int baseValue_);
    }
}
