using System;
using System.Collections.Generic;
using _DreamKeep.CommonPresentation;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>选牌结算特效根基类：段内订阅本段完成事实后调用，落点与间距在此统一配置。
    /// 段内特效应走各段自己的根，不使用常驻的 VfxDefaultRoot。</summary>
    public abstract class CardOperationVfxRoot : MonoBehaviour
    {
        [Header("选牌结算演出")] [Tooltip("横向均布的落点父级：接段画布中心容器")] [SerializeField]
        private Transform _playVfxParent;

        [Tooltip("多张卡之间的横向间距（画布像素）")] [SerializeField]
        private float _vfxSpacingPx = 500f;

        [Tooltip("演出整体缩放")] [SerializeField] private float _vfxScale = 1f;

        protected UniTask PlayAsync(VfxFactory vfxFactory_, IReadOnlyList<CardModel> cards_, bool upgraded_) =>
            PlayAsync(vfxFactory_, _playVfxParent ? _playVfxParent : transform, _vfxSpacingPx,
                _vfxScale, cards_, upgraded_);

        private static UniTask PlayAsync(VfxFactory vfxFactory_, Transform parent_, float spacingPx_, float scale_,
            IReadOnlyList<CardModel> cards_, bool upgraded_)
        {
            if (cards_.Count == 0) return UniTask.CompletedTask;
            UniTaskCompletionSource done = new();
            Vector3 center = parent_.position;
            for (int i = 0; i < cards_.Count; i++)
            {
                float shift = (i - (cards_.Count - 1) / 2f) * spacingPx_;
                Vector3 worldPos = center + parent_.TransformVector(new Vector3(shift, 0f, 0f));
                Action onFinished = () => done.TrySetResult();
                if (upgraded_) vfxFactory_.PlaySmithForge(parent_, worldPos, scale_, cards_[i], onFinished);
                else vfxFactory_.PlayRemoveCard(parent_, worldPos, scale_, cards_[i], onFinished);
            }

            return done.Task;
        }
    }
}