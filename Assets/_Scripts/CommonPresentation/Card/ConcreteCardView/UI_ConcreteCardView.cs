using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>统一卡牌详情特写：全屏黑背板（点空处关闭）+ 大卡展示 + 升级预览开关行。
    /// 卡组面板（左/右键）、三选一（右键）、锻造（右键）共用这一个预制体，各消费方把它实例化在自己名下，
    /// 层级跟随发起者所在画布，不会盖到更高层的顶栏。展示口径归开关行（勾=升级后形态、不勾=基础版或当前），
    /// 纯展示、永不写回域。</summary>
    [RequireComponent(typeof(Button))]
    public sealed class UI_ConcreteCardView : MonoBehaviour
    {
        [Tooltip("特写卡：全尺寸展示当前查看的卡")] [SerializeField] private UI_CardInDeck _closeupCard;

        [Tooltip("升级预览开关行：显隐与展示口径由它判定")] [SerializeField]
        private UI_UpgradePreviewToggle _upgradePreview;

        private void Awake()
        {
            // 预制体零接线可跑：特写卡与开关行都在本层级的固定槽位上，没拖引用就按类型自愈取件
            if (!_closeupCard) _closeupCard = GetComponentInChildren<UI_CardInDeck>(true);
            if (!_upgradePreview) _upgradePreview = GetComponentInChildren<UI_UpgradePreviewToggle>(true);
            _upgradePreview.Changed += Refresh;
            GetComponent<Button>().onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (_upgradePreview) _upgradePreview.Changed -= Refresh;
        }

        #region 对外：开合特写

        /// <summary>打开指定卡的详情：先开层（首次激活在此补跑 Awake 接好监听）再摆内容，压住同父级其余内容。</summary>
        public void Open(CardModel card_)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _upgradePreview.SetCard(card_);
            Refresh();
        }

        /// <summary>收起特写：纯展示层无状态，下次 Open 全量重摆。</summary>
        public void Close() => gameObject.SetActive(false);

        #endregion

        // 特写刷新：展示口径归开关行，这里只重绑
        private void Refresh() => _closeupCard.Bind(_upgradePreview.DisplayCard, null);
    }
}
