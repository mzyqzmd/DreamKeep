using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>瞄准客户端：同一时刻只允许一件东西在瞄准（手牌或药水），由 BattleTargetDirector 独占驱动。
    /// 客户端只提供事实（要不要解析、用不用指针、箭头起点、是否展开）并接收结果；落点规则一律来自玩法层只读查询，
    /// 表现层不重写规则，指针命中判定也只在玩法层给出的存活敌人里做。</summary>
    public interface IAimClient
    {
        /// <summary>是否正在瞄准：为假时中间件同帧收起箭头与高亮。</summary>
        bool IsAiming { get; }

        /// <summary>是否走指针指向解析：真则取指针命中的敌人下标，假则取稳定默认下标（落点不读槽位的客户端用）。</summary>
        bool UsesPointerTargeting { get; }

        /// <summary>箭头是否已展开：未展开时中间件只收箭头、不画。</summary>
        bool ArrowShown { get; }

        /// <summary>箭头起点（世界坐标）。</summary>
        Vector3 AimOrigin { get; }

        /// <summary>该下标会命中的目标集合：客户端只把玩家选择交给玩法层现算。</summary>
        IReadOnlyList<Creature> PreviewTargets(int slot_);

        /// <summary>接收本轮瞄准结果（目标下标与目标集合）并在客户端内部消费。</summary>
        void ApplyAimResult(int slot_, IReadOnlyList<Creature> targets_);
    }
}
