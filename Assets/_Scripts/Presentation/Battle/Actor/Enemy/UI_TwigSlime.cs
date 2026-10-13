namespace _DreamKeep.Presentation
{
    /// <summary>树枝史莱姆单位视图：四条公约动画（idle_loop/attack/hurt/die）走 UI_Actor，
    /// 本类只作为预制体的组件身份（twig_slime 的 cast 尚未接入演出）。</summary>
    public sealed class UI_TwigSlime : UI_Enemy
    {
        private const string CastAnim = "cast";
    }
}
