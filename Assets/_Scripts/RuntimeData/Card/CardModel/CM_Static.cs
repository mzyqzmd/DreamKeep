using System;
using _DreamKeep.ConfigData;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>卡面静态配置容器：烘焙期由 CardModelBaker 一次性填装，运行时只读。
    /// 持有卡牌不随对局变化的配置数据（图标、类型、稀有度、目标规则、是否可升级），
    /// 供 UI 与逻辑统一查询，无需回源 SO_Card。</summary>
    internal class CM_Static
    {
        internal Sprite Icon { get; }
        internal ECardType Type { get; }
        internal ECardRarity Rarity { get; }
        internal ECardTargetType TargetType { get; } //对外不需要知道目标类型枚举
        internal bool CanUpgrade { get; }

        internal CM_Static(Sprite icon_, ECardType type_, ECardRarity rarity_, ECardTargetType targetType_,
            bool canUpgrade_)
        {
            Icon = icon_;
            Type = type_;
            Rarity = rarity_;
            TargetType = targetType_;
            CanUpgrade = canUpgrade_;
        }

        internal string GetTypeDescription() =>
            Type switch
            {
                ECardType.Attack => "攻击",
                ECardType.Skill => "技能",
                ECardType.Ability => "能力",
                ECardType.Curse => "诅咒",
                _ => throw new ArgumentOutOfRangeException()
            };

        internal bool IsNoneTarget => TargetType == ECardTargetType.None;
        internal bool IsSelfTarget => TargetType == ECardTargetType.Player;
        internal bool IsFrontEnemyTarget => TargetType == ECardTargetType.FrontEnemy;
        internal bool IsAnyEnemyTarget => TargetType == ECardTargetType.AnyEnemy;
        internal bool IsRandomEnemyTarget => TargetType == ECardTargetType.RandomEnemy;
        internal bool IsAllEnemyTarget => TargetType == ECardTargetType.AllEnemy;
    }
}