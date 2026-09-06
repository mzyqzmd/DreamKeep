namespace _DreamKeep.Data
{
    /// <summary>词条种类：描述一个词条是产出动作的效果型，还是只参与出牌前落点解析的修饰型。</summary>
    public enum EAffixKind
    {
        //目标类
        Aim,              //指定
        Pierce,           //穿透

        //伤害/格挡
        Damage,           //伤害
        Block,            //格挡

        //辅助
        DrawCard,         //抽牌
        GainEnergy,       //获得能量

        //特殊
        ApplyStatus,      //施加状态
        Grant,            //给予
        Exhaust,          //消耗
        Erase,            //遗忘
        Retain,           //保留
    }
}
