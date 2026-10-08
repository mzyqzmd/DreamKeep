namespace _DreamKeep.Presentation
{
    /// <summary>树枝史莱姆单位视图：四条公约动画（idle_loop/attack/hurt/die）走 UI_Actor，
    /// 本类只声明 twig_slime 的专属动画（同时是预制体的组件身份）。</summary>
    public class UI_TwigSlime : UI_Enemy
    {
        // twig_slime 专属动画：cast 尚未接入演出
        private const string CastAnim = "cast";
    }
}
