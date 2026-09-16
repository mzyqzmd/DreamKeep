namespace _DreamKeep.Presentation
{
    /// <summary>卡牌能力契约：能力组件被挂到卡牌上后由卡牌根统一回调，认领卡牌根并从根取所需上下文。
    /// 有该组件才有该效果——战斗卡挂拖拽能力，展示卡不挂即为纯展示。</summary>
    public interface ICardAbility
    {
        void Attach(Obj_Card card_);
    }
}
