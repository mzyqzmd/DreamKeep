using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>战场槽位区：持有纵深阵型四个槽位点（0 号为玩家、1~3 号为敌人，1 最靠前、3 最靠后），
    /// 是单位出生摆位与移动补间的唯一坐标来源；槽位编号口径见 RuntimeData 的 BattleSlotLayout。</summary>
    public class UI_ActorSlotGroup : MonoBehaviour
    {
        [SerializeField] private Transform _slot0;
        [SerializeField] private Transform _slot1;
        [SerializeField] private Transform _slot2;
        [SerializeField] private Transform _slot3;

        #region 按槽位号取槽位点：单位出生与移动都向它查询

        public Transform GetSlotTransform(int slot_) => slot_ switch
        {
            0 => _slot0,
            1 => _slot1,
            2 => _slot2,
            3 => _slot3,
            _ => null,
        };

        #endregion
    }
}
