using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>会员卡：持有后商店所有商品打折；本身不提供战斗效果，折扣系数由 RunRelicQueries 汇总后注入商店段模型。</summary>
    [CreateAssetMenu(fileName = "MembershipCard", menuName = "DreamKeep/Data/Relic/会员卡")]
    public sealed class SO_MembershipCard : SO_Relic
    {
        [Range(0f, 1f)] [SerializeField] private float _discount = 0.6f;

        /// <summary>折扣比例：0.6 表示优惠 60%，实付为原价的 40%。</summary>
        public float Discount => _discount;
    }
}
