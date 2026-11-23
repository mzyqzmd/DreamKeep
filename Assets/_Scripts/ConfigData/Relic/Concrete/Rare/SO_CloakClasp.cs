using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>斗篷扣：回合结束时按手牌张数获得格挡。</summary>
    [CreateAssetMenu(fileName = "CloakClasp", menuName = "DreamKeep/Data/Relic/斗篷扣")]
    public sealed class SO_CloakClasp : SO_Relic
    {
        [SerializeField] private int _blockPerCard = 1;

        public int BlockPerCard => _blockPerCard;
    }
}
