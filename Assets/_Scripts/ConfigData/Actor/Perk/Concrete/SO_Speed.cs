using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「速度」定义：层数即同步给宿主的真实敏捷层，回合结束整条移除并扣回。</summary>
    [CreateAssetMenu(fileName = "Speed", menuName = "DreamKeep/Data/Perk/速度")]
    public sealed class SO_Speed : SO_Perk
    {
        [SerializeField] private SO_Dexterity _dexterity;

        public SO_Dexterity Dexterity => _dexterity;

        public override string Name => "速度";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
