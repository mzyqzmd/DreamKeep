using System;
using System.Threading;
using _DreamKeep.EngineUtility;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>
    /// 牌堆视图基类：收口抽牌堆 / 弃牌堆 / 消耗堆共用的计数文本与"有牌进堆"的反馈——
    /// 进堆 / 离堆都只是入队，写出显示数的节奏由本类自己逐张推进（每张间隔与整段时长上限都是本类的旋钮），
    /// 故"一帧内到一批"也会逐张跳而不是一步到终值；批量来路可用 StepArriveAsync / StepLeaveAsync 等整段跳完。
    ///</summary>
    public abstract class UI_Pile : MonoBehaviour
    {
        [Tooltip("计数文本：显示本堆张数")] [SerializeField]
        protected TextMeshProUGUI _countText;

        [Header("进堆弹跳")] [Tooltip("弹跳幅度：0.15 即最多放大 15%")] [SerializeField]
        private float _popScale = 0.15f;

        [Tooltip("放大到峰值用的时长（秒）：极短，起来要快")] [SerializeField]
        private float _popUpDuration = 0.05f;

        [Tooltip("从当前缩放回落到基准缩放的时长（秒）：减速轻着地")] [SerializeField]
        private float _popDownDuration = 0.12f;

        [Header("逐张步进")] [Tooltip("逐张跳动时每张之间的间隔（秒）")] [SerializeField]
        private float _stepInterval = 0.04f;

        [Tooltip("整段逐张跳动的总时长上限（秒）：张数多时按 剩余时长 / 剩余张数 压缩每张间隔")] [SerializeField]
        private float _stepMaxDuration = 0.6f;

        protected abstract int LiveCount { get; } // 派生类给活值（表达式主体）

        private int _shown;
        private int _pending; // 待跳队列：正数为待进堆、负数为待离堆，逐张写数由下面这段节奏推进
        private bool _draining;
        private UniTaskCompletionSource _drained;
        private Vector3 _baseScale;
        private Tween _punchTween;

        private void Awake() => _baseScale = transform.localScale;
        private void OnDestroy() => _punchTween?.Kill();

        // 进堆一张：入队一张，起手当帧就写数并弹；已有一段节奏在跑时这张排在队尾跟着跳
        public void OnCardArrived() => Enqueue(1);

        // 离堆一张：入队一张，起手当帧就写数、不弹
        public void OnCardLeft() => Enqueue(-1);

        public void SyncToLive() // 显示数直接对齐活值、清空待跳队列、不弹（构造初值 + 回合兜底共用）
        {
            _pending = 0;
            _shown = LiveCount;
            _countText.text = _shown.ToString();
        }

        // 逐张进堆：入队 count_ 张，等整段跳完才返回——批量来路（洗回、弃牌波）据此把域流程压在节奏之后
        public async UniTask StepArriveAsync(int count_)
        {
            Enqueue(count_);
            if (_draining) await _drained.Task;
        }

        // 逐张离堆：入队 count_ 张，等整段跳完才返回
        public async UniTask StepLeaveAsync(int count_)
        {
            Enqueue(-count_);
            if (_draining) await _drained.Task;
        }

        // 入队：正数进、负数出；原本没有节奏在跑才起一段——起手那一张是同步跳的，故单张来路仍旧当帧写数
        private void Enqueue(int delta_)
        {
            _pending += delta_;
            if (!_draining) DrainAsync().Forget();
        }

        // 一段节奏：逐张把待跳队列写进显示数——进堆那张写数并弹、离堆那张只写数；
        // 跳与跳之间取"剩余时长 / 剩余张数"（不超过 _stepInterval），整段不超过 _stepMaxDuration；
        // 队列被 SyncToLive 清空、或宿主途中销毁时立即收工，等待方一并放行
        private async UniTask DrainAsync()
        {
            // 销毁令牌只在宿主活着时取一次：对象被销毁后再取会抛 MissingReferenceException（停 Play 打断在途节奏时命中）
            CancellationToken destroyToken = destroyCancellationToken;
            _draining = true;
            _drained = new UniTaskCompletionSource();
            float start = Time.time;
            while (_pending != 0 && !destroyToken.IsCancellationRequested)
            {
                bool arriving = _pending > 0;
                _pending += arriving ? -1 : 1;
                _shown = arriving ? Mathf.Min(_shown + 1, LiveCount) : Mathf.Max(_shown - 1, LiveCount);
                _countText.text = _shown.ToString();
                // TODO 进堆音效：牌进堆的反馈归本堆自己，将来在此处播放
                if (arriving)
                    UiTween.PlayPunch(ref _punchTween, transform, _baseScale, _popScale, _popUpDuration,
                        _popDownDuration);
                if (_pending == 0) break;
                float remain = Mathf.Max(_stepMaxDuration - (Time.time - start), 0f);
                float step = Mathf.Min(_stepInterval, remain / Mathf.Abs(_pending));
                await UniTask.Delay(TimeSpan.FromSeconds(step), cancellationToken: destroyToken)
                    .SuppressCancellationThrow();
            }

            _draining = false;
            _drained.TrySetResult();
        }
    }
}
