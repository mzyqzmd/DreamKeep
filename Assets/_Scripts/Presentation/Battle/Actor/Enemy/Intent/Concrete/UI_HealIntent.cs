using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation.Intent
{
    /// <summary>治疗意图图标：纯图标展示，数值不对外展示（占位视图，将来有专属表现再补）。</summary>
    public sealed class UI_HealIntent : UI_Intent
    {
        /// <summary>绑定治疗意图：视觉暂无专属内容，只需登记意图供悬停取说明。</summary>
        public void Bind(EI_Heal heal_) => BindIntent(heal_);
    }
}
