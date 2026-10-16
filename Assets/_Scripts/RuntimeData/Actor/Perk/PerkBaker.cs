using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态烘焙器：把配置模板烘成运行时实例，参数在烘焙期读一次；未登记类型静默跳过。</summary>
    public static class PerkBaker
    {
        #region 按配置烘焙一个运行时状态实例，未登记类型返回 null

        public static BattlePerk Bake(PerkData data_) => data_ switch
        {
            PD_Vulnerable vulnerable => new BP_Vulnerable(vulnerable),
            PD_Weak weak => new BP_Weak(weak),
            PD_Strength strength => new BP_Strength(strength),
            PD_Burn burn => new BP_Burn(burn),
            PD_Regen regen => new BP_Regen(regen),
            _ => null,
        };

        #endregion
    }
}
