namespace _DreamKeep.ConfigData
{
    /// <summary>遭遇种类：决定从怪物池与背景池的哪一档取候选。只服务于"抽遭遇 / 抽背景"，
    /// 商店、休息、宝箱这类非战斗房间不属于本枚举。</summary>
    public enum EEncounterKind
    {
        Normal,
        Elite ,
        Boss,
    }
}
