using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace _DreamKeep.Presentation
{
    /// <summary>卡牌根：卡牌表现的唯一门面，也是每张卡的本体必备组件——刷新卡面内容与外框配色、接入宿主、认领能力、下达落位与层序、播放入场/排队/离场动效。
    /// 组件粒度判据：Obj_ = 必备本体（卡面、落位、动效都收在此）；Comp_ = 可选能力——能力由预制体配置决定，运行时只认领编辑期挂上的组件，不挂即为纯展示（将来卡牌图鉴复用本预制体且不挂拖拽，就是无拖拽的展示卡）。
    /// 运行期只有 Bind → SetHost 两步，不再动态添加能力；Attach 只认领卡牌根，模型与宿主由能力按需读取。
    /// 位置/朝向/层序经 Place 下达，缩放由 ScaleToAsync 或瞬时直写 transform.localScale 承接；补间按通道独占，重开前先 Kill 同通道。
    /// 位置通道只有一个入口：带层序的滑翔落位——排布、出牌区排队、悬停拎起与归宿飞行都用它，谁开播谁写的层序随补间一起落定。</summary>
    public class Obj_Card : MonoBehaviour
    {
        [Tooltip("整卡的排序单元：卡面各文本与图标都挂在它名下，层序只由卡牌根一处改它，其它代码不碰")] [SerializeField]
        private SortingGroup _sortingGroup;

        [Tooltip("卡面费用文本，内容来自 CardModel.Cost")] [SerializeField]
        private TextMeshPro _cost;

        [Tooltip("卡面名称文本，内容来自 CardModel.Name")] [SerializeField]
        private TextMeshPro _name;

        [Tooltip("卡面类型文本，内容来自 CardModel.GetTypeDescription()")] [SerializeField]
        private TextMeshPro _type;

        [Tooltip("卡面描述文本，内容来自 CardModel.GetDescription()")] [SerializeField]
        private TextMeshPro _description;

        [Tooltip("卡面图标，内容来自 CardModel.Icon，由 SpriteRenderer 承接")] [SerializeField]
        private SpriteRenderer _icon;

        [Tooltip("卡牌外框渲染器，颜色在 Bind 时按稀有度写入，外框图使用灰度基底供染色")] [SerializeField]
        private SpriteRenderer _frame;

        [Tooltip("普通卡外框色")] [SerializeField]
        private Color _frameColorNormal = new(0.82f, 0.84f, 0.88f);

        [Tooltip("稀有卡外框色")] [SerializeField]
        private Color _frameColorRare = new(0.45f, 0.72f, 1f);

        [Tooltip("超稀有卡外框色")] [SerializeField]
        private Color _frameColorSuperRare = new(1f, 0.84f, 0.42f);

        [Tooltip("不可打时整卡乘的暗灰色：与卡面本色相乘写回，稀有度外框差异得以保留")] [SerializeField]
        private Color _unplayableTint = new(0.45f, 0.45f, 0.5f);

        private Collider2D _body;
        private SpriteRenderer[] _faceSprites;
        private TextMeshPro[] _faceTexts;
        private Color[] _spriteColors;
        private Color[] _textColors;
        private Color _frameBase;
        private Tween _moveTween;
        private Tween _scaleTween;
        private bool _playable = true;

        public CardModel Card { get; private set; }

        public Obj_HandView Host { get; private set; }

        /// <summary>当前层序，供宿主裁决悬停时比较谁在最上层。</summary>
        public int Order => _sortingGroup.sortingOrder;

        /// <summary>是否有位置补间在飞：悬停闸门用，飞行中的牌不响应悬停，避免层序被补间逐帧覆盖。</summary>
        public bool IsMoving => _moveTween != null && _moveTween.IsActive();

        /// <summary>指针命中查询：悬停裁决由宿主逐帧调用；未配卡面碰撞体的卡（无需指针交互）恒不命中。</summary>
        public bool ContainsPoint(Vector3 point_) => _body && _body.OverlapPoint(point_);

        // 认领编辑期配在卡上的能力：有组件才有该效果，未配能力的卡（如卡牌图鉴复用本预制体）不挂即为纯展示；
        // 顺带收齐卡面全部渲染器与它们的编辑期本色，不可打外观的乘色以本色为基准，可打时逐字写回
        private void Awake()
        {
            _body = GetComponent<Collider2D>();
            _faceSprites = GetComponentsInChildren<SpriteRenderer>(true);
            _faceTexts = GetComponentsInChildren<TextMeshPro>(true);
            _spriteColors = new Color[_faceSprites.Length];
            _textColors = new Color[_faceTexts.Length];
            for (int i = 0; i < _faceSprites.Length; i++) _spriteColors[i] = _faceSprites[i].color;
            for (int i = 0; i < _faceTexts.Length; i++) _textColors[i] = _faceTexts[i].color;
            foreach (ICardAbility ability in GetComponents<ICardAbility>())
                ability.Attach(this);
        }

        #region 绑定卡面：刷新显示并把运行时卡模型收在卡牌根上

        public void Bind(CardModel card_)
        {
            Card = card_;
            _name.text = card_.Name;
            _type.text = card_.GetTypeDescription();
            _icon.sprite = card_.Icon;
            _cost.text = card_.Cost.Value.ToString();
            _description.text = card_.GetDescription();
            _frameBase = card_.Rarity switch
            {
                ECardRarity.Rare => _frameColorRare,
                ECardRarity.SuperRare => _frameColorSuperRare,
                _ => _frameColorNormal
            };
            // 外框色随稀有度变，统一由 SetPlayableLook 写；池复用后残留的压暗色与旧卡本色借此一并刷正
            SetPlayableLook(_playable);
        }

        #endregion

        #region 不可打外观：整卡渲染器与文本乘同一个灰调，可打时写回本色

        public void SetPlayableLook(bool playable_)
        {
            _playable = playable_;
            for (int i = 0; i < _faceSprites.Length; i++)
                _faceSprites[i].color = playable_ ? _spriteColors[i] : _spriteColors[i] * _unplayableTint;
            for (int i = 0; i < _faceTexts.Length; i++)
                _faceTexts[i].color = playable_ ? _textColors[i] : _textColors[i] * _unplayableTint;
            // 外框色由 Bind 存进 _frameBase，最后单独覆盖，避免被上面那批编辑期本色写回灰度基底
            _frame.color = playable_ ? _frameBase : _frameBase * _unplayableTint;
        }

        #endregion

        public void SetHost(Obj_HandView host_) => Host = host_;

        #region 落位：设置世界坐标、朝向与层序，位置/朝向/层序的唯一写入通道，能力不直接写 transform 与 SortingGroup

        public void Place(Vector3 position_, Quaternion rotation_, int order_)
        {
            transform.position = position_;
            transform.rotation = rotation_;
            _sortingGroup.sortingOrder = order_;
        }

        #endregion

        #region 动效：位置与缩放两通道补间，重开前先 Kill 同通道

        // 滑翔落位：位置与朝向按补间推进，层序随同落定
        public UniTask MoveToAsync(Vector3 position_, Quaternion rotation_, int order_, float duration_) =>
            MoveAsync(position_, rotation_, order_, duration_);

        // 位置通道实现：排布、悬停与归宿三套写入共用它，各自开播前先 Kill 同通道，层序随补间一并落定
        private UniTask MoveAsync(Vector3 position_, Quaternion rotation_, int order_, float duration_)
        {
            _moveTween?.Kill();
            Vector3 fromPosition = transform.position;
            Quaternion fromRotation = transform.rotation;
            float progress = 0f;
            _moveTween = DOTween.To(() => progress, value_ => progress = value_, 1f, duration_)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => Place(Vector3.Lerp(fromPosition, position_, progress),
                    Quaternion.Slerp(fromRotation, rotation_, progress), order_));
            return WaitAsync(_moveTween);
        }

        public UniTask ScaleToAsync(float scale_, float duration_)
        {
            _scaleTween?.Kill();
            _scaleTween = DOTween.To(() => transform.localScale.x,
                    value_ => transform.localScale = Vector3.one * value_, scale_, duration_)
                .SetEase(Ease.OutQuad);
            return WaitAsync(_scaleTween);
        }

        #endregion

        #region 清干净播放的动画

        // 回收前统一清干净补间，防止补间继续写已被复用的对象
        public void KillMotion()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        #endregion

        #region DOTween → UniTask 等待胶水
        // 完成与被 Kill 都算结束，杜绝 await 悬挂（第二个消费者出现时再抽公共扩展）
        private static UniTask WaitAsync(Tween tween_)
        {
            if (!tween_.IsActive()) return UniTask.CompletedTask;
            UniTaskCompletionSource source = new();
            tween_.OnComplete(() => source.TrySetResult()).OnKill(() => source.TrySetResult());
            return source.Task;
        }

        #endregion
    }
}