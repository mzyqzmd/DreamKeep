using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>卡牌根：卡牌表现的唯一门面——刷新卡面、接入宿主、装配与查询能力、下达落位与层序、播放入场/排队/离场动效。
    /// 卡牌本体（视图）固定存在，交互效果由 ICardAbility 能力组件按需挂载，有组件才有该效果。
    /// 运行期装配顺序固定为 Bind → SetHost → AttachAbility；Attach 只认领卡牌根，模型与宿主由能力按需读取。
    /// 位置/朝向/层序与缩放/透明度是表现层唯一写入通道，补间按通道独占，重开前先 Kill 同通道。</summary>
    [RequireComponent(typeof(Comp_CardView))]
    public class Obj_Card : MonoBehaviour
    {
        private readonly HashSet<ICardAbility> _attachedAbilities = new();

        private Comp_CardView _view;
        private Tween _moveTween;
        private Tween _scaleTween;
        private Tween _fadeTween;

        public Comp_CardView View => _view;

        public CardModel Card { get; private set; }

        public Obj_HandView Host { get; private set; }

        public int SortingOrder => _view.SortingOrder;

        // 编辑期挂在卡上的能力在此统一认领，与运行期 AttachAbility 走同一条 Attach 路径
        private void Awake()
        {
            _view = GetComponent<Comp_CardView>();
            foreach (ICardAbility ability in GetComponents<ICardAbility>()) AttachOnce(ability);
        }

        #region 绑定卡面：刷新显示并把运行时卡模型收在卡牌根上

        public void Bind(CardModel card_)
        {
            Card = card_;
            _view.Bind(card_);
        }

        #endregion

        public void SetHost(Obj_HandView host_) => Host = host_;

        #region 装配能力：已挂则复用，且每个能力只回调一次 Attach

        public T AttachAbility<T>() where T : MonoBehaviour, ICardAbility
        {
            T ability = GetComponent<T>();
            if (ability == null) ability = gameObject.AddComponent<T>();
            AttachOnce(ability);
            return ability;
        }

        #endregion

        public T GetAbility<T>() where T : MonoBehaviour, ICardAbility => GetComponent<T>();

        #region 落位与动效：位置/朝向/层序与缩放/透明度的唯一写入通道，能力不直接写 transform 与 SortingGroup

        public void Place(Vector3 position_, Quaternion rotation_, int order_) => _view.Place(position_, rotation_, order_);

        public void SetScale(float scale_) => transform.localScale = Vector3.one * scale_;

        public void SetAlpha(float alpha_) => _view.SetAlpha(alpha_);

        // 滑翔落位：位置与朝向按补间推进，层序随同落定
        public UniTask MoveToAsync(Vector3 position_, Quaternion rotation_, int order_, float duration_)
        {
            _moveTween?.Kill();
            Vector3 fromPosition = transform.position;
            Quaternion fromRotation = transform.rotation;
            float progress = 0f;
            _moveTween = DOTween.To(() => progress, value => progress = value, 1f, duration_)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => Place(Vector3.Lerp(fromPosition, position_, progress), Quaternion.Slerp(fromRotation, rotation_, progress), order_));
            return WaitAsync(_moveTween);
        }

        public UniTask ScaleToAsync(float scale_, float duration_)
        {
            _scaleTween?.Kill();
            _scaleTween = DOTween.To(() => transform.localScale.x, value => SetScale(value), scale_, duration_).SetEase(Ease.OutQuad);
            return WaitAsync(_scaleTween);
        }

        public UniTask FadeToAsync(float alpha_, float duration_)
        {
            _fadeTween?.Kill();
            _fadeTween = DOTween.To(() => _view.Alpha, value => SetAlpha(value), alpha_, duration_).SetEase(Ease.OutQuad);
            return WaitAsync(_fadeTween);
        }

        #endregion

        // 回收前统一清干净补间，防止补间继续写已被复用的对象
        public void KillMotion()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _scaleTween?.Kill();
            _scaleTween = null;
            _fadeTween?.Kill();
            _fadeTween = null;
        }

        private void AttachOnce(ICardAbility ability_)
        {
            if (_attachedAbilities.Add(ability_)) ability_.Attach(this);
        }

        // DOTween → UniTask 等待胶水：完成与被 Kill 都算结束，杜绝 await 悬挂（第二个消费者出现时再抽公共扩展）
        private static UniTask WaitAsync(Tween tween_)
        {
            if (!tween_.IsActive()) return UniTask.CompletedTask;
            UniTaskCompletionSource source = new();
            tween_.OnComplete(() => source.TrySetResult()).OnKill(() => source.TrySetResult());
            return source.Task;
        }
    }
}
