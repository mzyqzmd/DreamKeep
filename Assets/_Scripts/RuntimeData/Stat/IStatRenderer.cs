namespace _DreamKeep.RuntimeData
{
    /// <summary>状态修正后的显示值渲染端口：词条把基础值与所属数值通道交给它，拿回该显示的文本片段
    /// （修正求值与着色都在实现方，词条只声明自己是哪条通道）。</summary>
    public interface IStatRenderer
    {
        string Render(int baseValue_, EStatChannel channel_);
    }
}
