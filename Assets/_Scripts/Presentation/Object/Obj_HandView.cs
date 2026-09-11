using System.Collections.Generic;
using _DreamKeep.Data;
using _DreamKeep.Gameplay;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.Splines;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌视图：沿曲线排布手牌卡面，按领域事件生成与回收，并把拖拽结果转成打出命令。</summary>
    public class Obj_HandView : MonoBehaviour
    {
        private const int DragOrder = 100;

        [SerializeField] private Obj_Card _cardPrefab;
        [SerializeField] private SplineContainer _handSpline;
        [SerializeField] private Obj_DropZone[] _dropZones;
        [SerializeField] private int _baseOrder = 10;

        private readonly Dictionary<CardModel, Obj_Card> _views = new();

        private BattleLoop _loop;
        private SO_CardDatabase _database;
        private Obj_Card _dragging;

        #region 注入：由容器在装配期填入战斗循环与卡面配置源

        [Inject]
        public void Construct(BattleLoop loop_, SO_CardDatabase database_)
        {
            _loop = loop_;
            _database = database_;
        }

        #endregion

        #region 生成一张手牌卡面，已存在则忽略

        public void Spawn(CardModel card_)
        {
            if (_views.ContainsKey(card_)) return;

            Obj_Card view = Instantiate(_cardPrefab, transform);
            if (_database.TryGetCard(card_.CardId, out var config))
                view.Bind(card_, config);
            view.GetComponent<Obj_CardDrag>().Initialize(this);
            _views.Add(card_, view);
        }

        #endregion

        #region 回收一张手牌卡面

        public void Despawn(CardModel card_)
        {
            if (!_views.TryGetValue(card_, out Obj_Card view)) return;
            _views.Remove(card_);
            Destroy(view.gameObject);
        }

        #endregion

        #region 按当前手牌顺序沿曲线重排

        public void Relayout()
        {
            CardPile hand = _loop.BattleState.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                CardModel card = hand.Cards[i];
                if (!_views.TryGetValue(card, out Obj_Card view)) continue;
                if (view == _dragging) continue;
                view.Place(SamplePosition(i, hand.Count), SampleRotation(i, hand.Count), _baseOrder + i);
            }
        }

        #endregion

        #region 开始拖拽：抬高到最上层

        public void BeginDrag(Obj_Card card_)
        {
            _dragging = card_;
            card_.Place(card_.transform.position, Quaternion.identity, DragOrder);
        }

        #endregion

        #region 拖拽中：跟随指针并高亮命中的落点

        public void UpdateDrag(Obj_Card card_, Vector3 worldPosition_)
        {
            _dragging = card_;
            card_.Place(worldPosition_, Quaternion.identity, DragOrder);
            Highlight(GetZoneAt(worldPosition_));
        }

        #endregion

        #region 结束拖拽：命中落点则请求打出，否则回位

        public void EndDrag(Obj_Card card_)
        {
            _dragging = null;
            Obj_DropZone zone = GetZoneAt(card_.transform.position);
            Highlight(null);
            if (zone != null)
                RequestPlay(card_.Card, zone.Slot);
            Relayout();
        }

        #endregion

        #region 请求打出：只发命令，结算结果由下一轮事件泵回收视图

        public void RequestPlay(CardModel card_, int aimSlot_)
        {
            if (!_loop.BattleState.Hand.Contains(card_)) return;
            _loop.ExecuteCommand(new IBC_PlayCard(card_, aimSlot_));
        }

        #endregion

        #region 查找覆盖该世界坐标的落点，没有返回 null

        private Obj_DropZone GetZoneAt(Vector3 worldPosition_)
        {
            for (int i = 0; i < _dropZones.Length; i++)
                if (_dropZones[i].Contains(worldPosition_))
                    return _dropZones[i];
            return null;
        }

        #endregion

        private void Highlight(Obj_DropZone zone_)
        {
            for (int i = 0; i < _dropZones.Length; i++)
                _dropZones[i].SetHighlight(_dropZones[i] == zone_);
        }

        // 单张手牌取曲线中点，避免除零
        private Vector3 SamplePosition(int index_, int count_) => _handSpline.EvaluatePosition(Ratio(index_, count_));

        private Quaternion SampleRotation(int index_, int count_)
        {
            Vector3 tangent = _handSpline.EvaluateTangent(Ratio(index_, count_));
            return Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
        }

        private static float Ratio(int index_, int count_) => count_ <= 1 ? 0.5f : index_ / (float)(count_ - 1);
    }
}