using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>商人台词表：成交与两种支付失败各一组，每组说几次由分组自身与次数台账决定。</summary>
    [CreateAssetMenu(fileName = "MerchantSpeechLines", menuName = "DreamKeep/Data/Speech/MerchantSpeechLines")]
    public sealed class SO_MerchantSpeechLines : ScriptableObject
    {
        [Header("购买成功")] [SerializeField] private SpeechLineGroup _purchaseSuccess = new();

        [Header("支付失败")] [Tooltip("金币不足（含删牌服务）")] [SerializeField]
        private SpeechLineGroup _failureGold = new();

        [Tooltip("药水栏已满")] [SerializeField] private SpeechLineGroup _failureSpace = new();

        public SpeechLineGroup PurchaseSuccess => _purchaseSuccess;
        public SpeechLineGroup FailureGold => _failureGold;
        public SpeechLineGroup FailureSpace => _failureSpace;
    }
}