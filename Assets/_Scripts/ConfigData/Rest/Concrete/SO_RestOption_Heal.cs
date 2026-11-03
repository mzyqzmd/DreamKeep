using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>美容觉：按最大生命比例回复生命（向上取整），说明由段入口按比例与实际回复量插值。</summary>
    [CreateAssetMenu(fileName = "Heal", menuName = "DreamKeep/Data/RestOption/美容觉")]
    public sealed class SO_RestOption_Heal : SO_RestOption
    {
        [Tooltip("回复比例（占最大生命）")] [SerializeField] private float _healMaxHpRatio = 0.3f;

        public float HealMaxHpRatio => _healMaxHpRatio;
    }
}
