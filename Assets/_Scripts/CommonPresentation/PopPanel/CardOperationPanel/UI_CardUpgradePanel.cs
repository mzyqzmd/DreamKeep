using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>锻造弹板：在基类的选卡流程之上多一层对照页——第一层列出牌库中可升级的卡，左键多选（选满目标张数时
    /// 确认键弹出）、右键开统一卡牌详情特写；第二层按选中顺序把「当前形态 / 升级后」两排卡一一对照，只预览不写域，
    /// 确认才逐张升级、关板并横向均布锻造演出。
    /// 目标张数 = min(请求张数, 可升级卡数)：为 0 时列表为空且确认键开板即弹出，点它直接完成（不演出、交还空列表），
    /// 保证「没有可升级卡」的事件选项也能推进流程。</summary>
    public sealed class UI_CardUpgradePanel : UI_CardSelectPanel
    {
        [Header("提示")] [Tooltip("按目标张数写的操作提示")] [SerializeField]
        private TMP_Text _tipText;

        [Header("对照预览页")] [Tooltip("第二层根节点：全屏黑底，点空白退回第一层")] [SerializeField]
        private GameObject _previewRoot;

        [SerializeField] private Button _previewCloseButton;
        [SerializeField] private Button _previewConfirmButton;

        [Tooltip("当前形态那一排：运行时按选中顺序生成卡视图")] [SerializeField]
        private Transform _unupgradeGroup;

        [Tooltip("升级后形态那一排：与上一排同序一一对应")] [SerializeField]
        private Transform _upgradedGroup;

        private MonoObjectPool<UI_CardInDeck> _unupgradePool; // 第二层「当前形态」
        private MonoObjectPool<UI_CardInDeck> _upgradedPool; // 第二层「升级后」
        private readonly List<UI_CardInDeck> _unupgradeViews = new();
        private readonly List<UI_CardInDeck> _upgradedViews = new();

        protected override void OnAwake()
        {
            if (_previewCloseButton) _previewCloseButton.onClick.AddListener(ClosePreview);
            if (_previewConfirmButton) _previewConfirmButton.onClick.AddListener(OnPreviewConfirm);
            // 预览页的黑底整面 Button 就在根节点上：点空白只退回第一层，不当作取消
            if (_previewRoot && _previewRoot.TryGetComponent(out Button backdrop))
                backdrop.onClick.AddListener(ClosePreview);
        }

        protected override void OnDestroyed()
        {
            if (_previewCloseButton) _previewCloseButton.onClick.RemoveListener(ClosePreview);
            if (_previewConfirmButton) _previewConfirmButton.onClick.RemoveListener(OnPreviewConfirm);
        }

        protected override void OnConstructed()
        {
            _unupgradePool = CreateCardPool(_unupgradeGroup);
            _upgradedPool = CreateCardPool(_upgradedGroup);
        }

        // 候选 = 牌库里还能升级的卡：已升级的与不可升级的都不进列表
        protected override IReadOnlyList<CardModel> CollectCandidates()
        {
            List<CardModel> cards = new();
            foreach (CardModel card in Player.Deck.Cards)
                if (card.CanUpgrade) cards.Add(card);
            return cards;
        }

        protected override void OnShownExtra()
        {
            if (!_tipText) return;
            _tipText.text = Target > 0
                ? $"选择 {Target} 张卡牌进行升级（右键查看详情）"
                : "没有可升级的卡牌，点击确认继续";
        }

        protected override void OnResetVisual() => ClosePreview();

        // 第一层确认：只翻到对照页、不动域
        protected override void OnConfirm() => OpenPreview();

        protected override void PlayVfx(Transform parent_, Vector3 worldPosition_, float scale_, CardModel card_,
            Action onFinished_)
        {
            if (Audio) Audio.Play(Audio.Cues.Forge);
            VfxFactory.PlaySmithForge(parent_, worldPosition_, scale_, card_, onFinished_);
        }

        // 对照页：按选中顺序生成两排卡——上排当前形态、下排升级副本（一次性预览，不写回域）
        private void OpenPreview()
        {
            if (_previewRoot) _previewRoot.SetActive(true);

            List<CardModel> originals = new(Selected.Count);
            List<CardModel> upgraded = new(Selected.Count);
            for (int i = 0; i < Selected.Count; i++)
            {
                CardModel card = Selected[i].Card;
                originals.Add(card);
                CardModel preview = card.ToMutable();
                preview.Upgrade();
                upgraded.Add(preview);
            }

            BindPreviewRow(_unupgradePool, _unupgradeViews, originals);
            BindPreviewRow(_upgradedPool, _upgradedViews, upgraded);
        }

        // 对照页退出（关闭键与黑底空白都走这里）：只回第一层，选中与涟漪原地保留
        private void ClosePreview()
        {
            if (_previewRoot) _previewRoot.SetActive(false);
            ReleasePreview(_unupgradePool, _unupgradeViews);
            ReleasePreview(_upgradedPool, _upgradedViews);
        }

        // 对照页确认：逐张升级真卡，随后由基类关板、横向均布锻造演出并交付
        private void OnPreviewConfirm()
        {
            if (Selected.Count == 0) return;
            List<CardModel> upgraded = TakeSelectedCards();
            for (int i = 0; i < upgraded.Count; i++) upgraded[i].Upgrade();
            FinishAsync(upgraded).Forget();
        }

        // 一排对照卡：多退少补后按序绑定；纯展示（无点击回调），关掉自驱悬停缩放任整行等比适配
        private void BindPreviewRow(MonoObjectPool<UI_CardInDeck> pool_, List<UI_CardInDeck> views_,
            IReadOnlyList<CardModel> cards_)
        {
            for (int i = views_.Count - 1; i >= cards_.Count; i--)
            {
                pool_.Release(views_[i]);
                views_.RemoveAt(i);
            }

            for (int i = 0; i < cards_.Count; i++)
            {
                if (i >= views_.Count) views_.Add(CreatePreviewItem(pool_));
                UI_CardInDeck item = views_[i];
                item.transform.SetSiblingIndex(i);
                item.Bind(cards_[i], null);
            }
        }

        private UI_CardInDeck CreatePreviewItem(MonoObjectPool<UI_CardInDeck> pool_)
        {
            UI_CardInDeck item = pool_.Get(CardPrefab);
            if (item.TryGetComponent(out UI_HoverScaler scaler)) scaler.enabled = false;
            return item;
        }

        private void ReleasePreview(MonoObjectPool<UI_CardInDeck> pool_, List<UI_CardInDeck> views_)
        {
            if (pool_ == null) return;
            for (int i = views_.Count - 1; i >= 0; i--)
                if (views_[i]) pool_.Release(views_[i]);
            views_.Clear();
        }
    }
}
