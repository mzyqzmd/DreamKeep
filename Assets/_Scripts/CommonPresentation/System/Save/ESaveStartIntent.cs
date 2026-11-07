namespace _DreamKeep.Presentation
{
    /// <summary>一次场景重载要开哪一局：None 保持现状自动开局，NewGame 开新局，Continue 从存档还原。</summary>
    public enum ESaveStartIntent
    {
        None = 0,
        NewGame = 1,
        Continue = 2,
    }
}
