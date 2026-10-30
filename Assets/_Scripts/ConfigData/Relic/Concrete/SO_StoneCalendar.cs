using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>等我启动：指定回合的回合结束时对全体敌人造成伤害。</summary>
    [CreateAssetMenu(fileName = "StoneCalendar", menuName = "DreamKeep/Data/Relic/等我启动")]
    public sealed class SO_StoneCalendar : SO_Relic
    {
        [SerializeField] private int _turn = 30;
        [SerializeField] private int _damage = 520;

        public int Turn => _turn;
        public int Damage => _damage;
    }
}
