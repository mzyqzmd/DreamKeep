using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>商人台词表：成交与两种支付失败各一组，多条随机取一句；空组视为不说话。
    /// 开摊不说话——与《角色台词总览》真商人口径一致（真商人没有 welcome/openInventory 键）。</summary>
    [CreateAssetMenu(fileName = "MerchantSpeechLines", menuName = "DreamKeep/Data/Speech/MerchantSpeechLines")]
    public sealed class SO_MerchantSpeechLines : ScriptableObject
    {
        [Header("购买成功")] [SerializeField] private List<string> _purchaseSuccess = new();

        [Header("支付失败")] [Tooltip("金币不足（含删牌服务）")] [SerializeField]
        private List<string> _failureGold = new();

        [Tooltip("药水栏已满")] [SerializeField] private List<string> _failureSpace = new();

        public string PickPurchaseSuccess() => SpeechLinePicker.Pick(_purchaseSuccess);
        public string PickFailureGold() => SpeechLinePicker.Pick(_failureGold);
        public string PickFailureSpace() => SpeechLinePicker.Pick(_failureSpace);
    }
}
