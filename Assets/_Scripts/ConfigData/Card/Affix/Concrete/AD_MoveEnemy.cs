using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>位移词条：把目标在敌人槽位间推动若干格，正数推远（远离玩家）、负数拉近（靠近玩家）。
    /// 撞到别的敌人时撞晕对方，推到槽位尽头则本词条不生效。</summary>
    [Serializable]
    [AddTypeMenu("Affix/位移")]
    public sealed class AD_MoveEnemy : AffixData
    {
        [SerializeField] private int _offset = 1; // 正数推远、负数拉近，绝对值即格数

        public int Offset => _offset;
        public override string Name => "位移";
    }
}
