using _DreamKeep.Data;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态烘焙器：把状态配置数据转成运行时状态，层数由施加它的词条给出。</summary>
    public static class PerkBaker
    {
        public static InBattlePerk Bake(PerkData data_, int layers_)
        {
            return data_ switch
            {
                PD_Burn burn => new IBP_Burn(layers_, burn),
                PD_Regen regen => new IBP_Regen(layers_, regen),
                PD_Strength strength => new IBP_Strength(layers_, strength),
                PD_Vulnerable vulnerable => new IBP_Vulnerable(layers_, vulnerable),
                PD_Weak weak => new IBP_Weak(layers_, weak),
                _ => null,
            };
        }
    }
}
