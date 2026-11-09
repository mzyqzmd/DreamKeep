namespace _DreamKeep.Presentation
{
    /// <summary>整局终点种类：ResultPanel 按它选择文案组；新增终点（如主菜单「放弃」）在这里追加。</summary>
    public enum EResultKind
    {
        RunVictory,  // 整局通关：Boss 节点战斗胜利
        RunDefeated, // 整局战败：复活耗尽后的真死亡
    }
}
