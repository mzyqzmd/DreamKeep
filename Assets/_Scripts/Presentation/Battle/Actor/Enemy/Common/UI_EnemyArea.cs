using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>敌人槽位区：持有纵深阵型三个槽位点（0 最靠前、2 最靠后），是敌人出生摆位与移动补间的唯一坐标来源。</summary>
    public class UI_EnemyArea : MonoBehaviour
    {
        [SerializeField] private Transform _slot0;
        [SerializeField] private Transform _slot1;
        [SerializeField] private Transform _slot2;

        #region 按槽位号取槽位点：敌人出生与移动都向它查询

        public Transform GetSlotTransform(int slot_) => slot_ switch
        {
            0 => _slot0,
            1 => _slot1,
            2 => _slot2,
            _ => null,
        };

        #endregion
    }
}
