namespace _DreamKeep.GameBattle
{
    /// <summary>使用药水：携带槽位下标与指定目标下标；单体落点必须给合法目标，槽位与落点的合法性由 Domain 校验。</summary>
    public sealed class BC_UsePotion : BattleCommand
    {
        public int SlotIndex { get; }
        public int AimSlot { get; }

        public BC_UsePotion(int slotIndex_, int aimSlot_)
        {
            SlotIndex = slotIndex_;
            AimSlot = aimSlot_;
        }
    }
}
