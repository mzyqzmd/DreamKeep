using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态烘焙器：把状态定义资产烘成运行时实例，可改数值在烘焙期拷进实例；未登记类型静默跳过。</summary>
    public static class PerkBaker
    {
        #region 按定义烘焙一个运行时状态实例，未登记类型返回 null

        public static BattlePerk Bake(SO_Perk data_) => data_ switch
        {
            SO_Vulnerable vulnerable => new BP_Vulnerable(vulnerable),
            SO_Weak weak => new BP_Weak(weak),
            SO_Strength strength => new BP_Strength(strength),
            SO_Dexterity dexterity => new BP_Dexterity(dexterity),
            SO_Regen regen => new BP_Regen(regen),
            SO_Slow slow => new BP_Slow(slow),
            SO_Rampage rampage => new BP_Rampage(rampage),
            SO_Dizzy dizzy => new BP_Dizzy(dizzy),
            _ => null,
        };

        #endregion
    }
}
