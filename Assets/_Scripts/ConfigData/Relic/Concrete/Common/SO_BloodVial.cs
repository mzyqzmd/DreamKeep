using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>小血瓶：战斗开始时回复生命。</summary>
    [CreateAssetMenu(fileName = "BloodVial", menuName = "DreamKeep/Data/Relic/小血瓶")]
    public sealed class SO_BloodVial : SO_Relic
    {
        [SerializeField] private int _heal = 2;

        public int Heal => _heal;
    }
}
