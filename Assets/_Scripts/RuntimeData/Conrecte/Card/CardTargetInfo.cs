using System;
using _DreamKeep.Data;

namespace _DreamKeep.RuntimeData
{
    /// <summary>运行时落点信息：由 SO_Card.TargetType 在 RuntimeData 内翻译，避免 Gameplay 直接引用 Data 枚举。</summary>
    public readonly struct CardTargetInfo
    {
        public bool IsSelf { get; }
        public bool IsAllEnemies { get; }
        public bool IsRandom { get; }
        public bool NeedsAim { get; }

        private CardTargetInfo(bool isSelf_ = false, bool isAllEnemies_ = false,
            bool isRandom_ = false, bool needsAim_ = false)
        {
            IsSelf = isSelf_;
            IsAllEnemies = isAllEnemies_;
            IsRandom = isRandom_;
            NeedsAim = needsAim_;
        }

        #region 把配置层目标类型翻译为运行时落点布尔，翻译落在 RuntimeData 内，Gameplay 不接触 ECardTargetType

        public static CardTargetInfo From(ECardTargetType targetType_)
        {
            return targetType_ switch
            {
                ECardTargetType.Player => new CardTargetInfo(isSelf_: true),
                ECardTargetType.AllEnemy => new CardTargetInfo(isAllEnemies_: true),
                ECardTargetType.RandomEnemy => new CardTargetInfo(isRandom_: true),
                ECardTargetType.AnyEnemy => new CardTargetInfo(needsAim_: true),
                ECardTargetType.FrontEnemy => new CardTargetInfo(),
                _ => throw new ArgumentOutOfRangeException(nameof(targetType_), targetType_, null)
            };
        }

        #endregion
    }
}