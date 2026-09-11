using _DreamKeep.Data;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态烘焙器：把状态配置数据转成运行时状态，层数由施加它的词条给出。</summary>
    public static class PerkBaker
    {
        public static BattlePerk Bake(int layers_, PerkData data_)
        {
            return data_ switch
            {
                PD_Burn burn => new BP_Burn(layers_, burn),
                PD_Regen regen => new BP_Regen(layers_, regen),
                PD_Strength strength => new BP_Strength(layers_, strength),
                PD_Vulnerable vulnerable => new BP_Vulnerable(layers_, vulnerable),
                PD_Weak weak => new BP_Weak(layers_, weak),
                _ => null,
            };
        }
    }
}