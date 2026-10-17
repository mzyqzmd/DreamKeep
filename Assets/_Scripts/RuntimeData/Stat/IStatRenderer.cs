namespace _DreamKeep.RuntimeData
{
    /// <summary>状态修正后的显示值渲染端口：词条把基准值、当前值与所属数值通道交给它，拿回该显示的文本片段
    /// （修正求值与着色都在实现方，词条只声明自己是哪条通道）。
    /// 基准值是"被修正前"的数，当前值是"现在生效"的数：伤害与格挡由实现方经预览门面求当前值（词条两数传同一个值），
    /// 状态层数这类词条内静态比较的通道则直接比较两数。</summary>
    public interface IStatRenderer
    {
        string Render(int baseValue_, int value_, EStatChannel channel_);
    }
}
