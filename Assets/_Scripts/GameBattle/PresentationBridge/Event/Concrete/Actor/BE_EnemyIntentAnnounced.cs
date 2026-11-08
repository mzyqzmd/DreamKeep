using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人回合开始宣告：在回合开始阶段、动作入队之前广播的纯机制事实，表示本回合准备行动；
    /// 只带敌人、本回合意图序号与前槽/阶段标记，不带任何台词文本；表现层据此取词，查不到就保持沉默。
    /// 被击晕整回合跳过行动的敌人不发。</summary>
    public struct BE_EnemyIntentAnnounced : IBattleEvent
    {
        public Enemy Enemy { get; }
        public int IntentIndex { get; }
        public bool IsFrontSlot { get; }
        public bool IsPhaseTwo { get; }

        public BE_EnemyIntentAnnounced(Enemy enemy_, int intentIndex_, bool isFrontSlot_, bool isPhaseTwo_)
        {
            Enemy = enemy_;
            IntentIndex = intentIndex_;
            IsFrontSlot = isFrontSlot_;
            IsPhaseTwo = isPhaseTwo_;
        }
    }
}
