namespace _DreamKeep.RuntimeData
{
    /// <summary>状态修正后的显示值渲染端口：词条把基准值、当前值与所属数值通道交给它，拿回该显示的文本片段
    /// （修正求值与着色都在实现方，词条只声明自己是哪条通道）。
    /// 基准值是升级补丁生效前的数（ModifiableStat.BaseValue），当前值是现在生效的数：
    /// 伤害与格挡的最终值由实现方经预览门面叠加战斗内 perk 修正，其余通道直接比较两数。</summary>
    public interface IStatRenderer
    {
        string Render(int baseValue_, int value_, EStatChannel channel_);
    }
}
