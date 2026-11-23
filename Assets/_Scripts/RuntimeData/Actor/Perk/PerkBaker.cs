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
            SO_Shrink shrink => new BP_Shrink(shrink),
            SO_Strength strength => new BP_Strength(strength),
            SO_Dexterity dexterity => new BP_Dexterity(dexterity),
            SO_Regen regen => new BP_Regen(regen),
            SO_Slow slow => new BP_Slow(slow),
            SO_Territorial territorial => new BP_Territorial(territorial),
            SO_Rampage rampage => new BP_Rampage(rampage),
            SO_Rampart rampart => new BP_Rampart(rampart),
            SO_Dizzy dizzy => new BP_Dizzy(dizzy),
            SO_Slippery slippery => new BP_Slippery(slippery),
            SO_Imbalanced imbalanced => new BP_Imbalanced(imbalanced),
            SO_Fasten fasten => new BP_Fasten(fasten),
            SO_Barricade barricade => new BP_Barricade(barricade),
            SO_NoDraw noDraw => new BP_NoDraw(noDraw),
            SO_Colossus colossus => new BP_Colossus(colossus),
            SO_CrimsonMantle crimsonMantle => new BP_CrimsonMantle(crimsonMantle),
            SO_Cruelty cruelty => new BP_Cruelty(cruelty),
            SO_DarkEmbrace darkEmbrace => new BP_DarkEmbrace(darkEmbrace),
            SO_DemonForm demonForm => new BP_DemonForm(demonForm),
            SO_FeelNoPain feelNoPain => new BP_FeelNoPain(feelNoPain),
            SO_FlameBarrier flameBarrier => new BP_FlameBarrier(flameBarrier),
            SO_Hellraiser hellraiser => new BP_Hellraiser(hellraiser),
            SO_Inferno inferno => new BP_Inferno(inferno),
            SO_Pyre pyre => new BP_Pyre(pyre),
            SO_Vicious vicious => new BP_Vicious(vicious),
            SO_Ritual ritual => new BP_Ritual(ritual),
            SO_Radiance radiance => new BP_Radiance(radiance),
            SO_Thorns thorns => new BP_Thorns(thorns),
            SO_Muscle muscle => new BP_Muscle(muscle),
            SO_SpeedPerk speedPerk => new BP_Speed(speedPerk),
            SO_Shackling shackling => new BP_Shackling(shackling),
            SO_Plating plating => new BP_Plating(plating),
            SO_Buffer buffer => new BP_Buffer(buffer),
            SO_BlockNextTurn blockNextTurn => new BP_BlockNextTurn(blockNextTurn),
            SO_ClarityPerk clarityPerk => new BP_ClarityPerk(clarityPerk),
            SO_StableSerum stableSerum => new BP_StableSerum(stableSerum),
            SO_Gigantification gigantification => new BP_Gigantification(gigantification),
            SO_Duplication duplication => new BP_Duplication(duplication),
            _ => null,
        };

        #endregion
    }
}
