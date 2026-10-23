namespace _DreamKeep.RuntimeData
{
    /// <summary>战场纵深阵型的槽位编号真源：0 号恒为玩家，1~3 号为敌人（1 最靠前、3 最靠后）。
    /// 编号同时被敌人自身的展示规则读取，故住在 RuntimeData 而不是战斗层。</summary>
    public static class BattleSlots
    {
        public const int Player = 0;
        public const int FrontEnemy = 1;
        public const int RearEnemy = 3;
        public const int Count = RearEnemy + 1;
    }
}
