namespace _DreamKeep.Presentation
{
    /// <summary>树枝史莱姆单位视图：当前行为全部来自基类，保留类型作预制体身份与将来差异化演出的落点。
    /// 动画名对齐 twig_slime 骨架（二进制提取，待 SkeletonDataAsset Inspector 核对）。</summary>
    public class UI_TwigSlime : UI_Enemy
    {
        // twig_slime 骨架动画清单：uniblink/multiblink/eye_anger/drop（全是待机小动作，暂无战斗动画，留 null 禁用）
        protected override string StandAnim => "uniblink";
    }
}
