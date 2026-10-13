using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation.Intent
{
    /// <summary>前进意图图标：纯图标展示，说明（含"站最前时触发特殊意图"）由悬停说明气泡给出（占位视图，将来有专属表现再补）。</summary>
    public sealed class UI_MoveIntent : UI_Intent
    {
        /// <summary>绑定前进意图：视觉暂无专属内容，只需登记意图供悬停取说明。</summary>
        public void Bind(EI_Move move_) => BindIntent(move_);
    }
}
