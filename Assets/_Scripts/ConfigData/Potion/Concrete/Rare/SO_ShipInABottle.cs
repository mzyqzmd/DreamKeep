using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>瓶中船：立即获得格挡，并给自己施加下回合格挡状态。</summary>
    [CreateAssetMenu(fileName = "ShipInABottle", menuName = "DreamKeep/Data/Potion/瓶中船")]
    public sealed class SO_ShipInABottle : SO_Potion
    {
        [SerializeField] private int _block = 10;
        [SerializeField] private SO_BlockNextTurn _blockNextTurn;

        public int Block => _block;
        public SO_BlockNextTurn BlockNextTurn => _blockNextTurn;
        public override EPotionTargetType TargetType => EPotionTargetType.Self;
    }
}
