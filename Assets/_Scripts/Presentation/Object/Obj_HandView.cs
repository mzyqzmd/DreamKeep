using System.Collections.Generic;
using _DreamKeep.GameBattle;
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
        //  [SerializeField] private int _baseOrder = 10;

        private readonly Dictionary<CardModel, Obj_Card> _views = new();

        private BattleCommandDispatcher _dispatcher;
        private Obj_Card _dragging;

        [Inject]
        public void Construct(BattleCommandDispatcher dispatcher_)
        {
            _dispatcher = dispatcher_;
        }

        #region 打出请求：校验在手上后转发命令门面，不直接触碰战斗循环

        public void RequestPlay(CardModel card_, int aimSlot_)
        {
            if (!_views.ContainsKey(card_)) return;
            _dispatcher.PlayCard(card_, aimSlot_);
        }

        #endregion
    }
}