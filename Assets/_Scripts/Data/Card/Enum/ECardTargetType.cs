namespace _DreamKeep.Data
{
    public enum ECardTargetType
    {
        Player,                             // 玩家
        FrontEnemy,                         // 前方敌人
        AnyEnemy,                           // 任意敌人(玩家手动选择)
        AnyEnenmyAndBackEnemy,              // 随机敌人以及后方敌人
        AllEnemy,                           // 所有敌人
        RandomEnemy,                        // 随机敌人
    }
}
