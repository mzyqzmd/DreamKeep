using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>燃烧之血：战斗结束时回复生命。</summary>
    [CreateAssetMenu(fileName = "BurningBlood", menuName = "DreamKeep/Data/Relic/燃烧之血")]
    public sealed class SO_BurningBlood : SO_Relic
    {
        [SerializeField] private int _heal = 6;

        public int Heal => _heal;
    }
}
