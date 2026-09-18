using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌登记处：卡池、手牌列、模型到卡面的索引、悬停与拖拽能力缓存都由它保管——它只管"谁在手牌里"，卡面内容与动效仍归 Obj_Card。
    /// 一张牌从取出到回收的整段生命周期都从这里过：Spawn 一次完成取池、绑卡、接入宿主、进手牌列、标记入场中与认领悬停/拖拽能力；
    /// 离手与归宿则由 RemoveFromHand / Release 收尾。
    /// 能力只在取出时查一次组件，逐帧裁决与按下抓取的热路径零 GetComponent；回收时注销，池复用由下次登记覆盖写。
    /// 指针命中的最上层手牌也收在这里（ResolveTopCard）：命中测试与层序比较只此一处，悬停裁决与抓取裁决同源。</summary>
    public class HandCardRegistry
    {
        private readonly ObjectPool<Obj_Card> _cardPool;
        private readonly Obj_HandView _host;
        private readonly List<Obj_Card> _hand = new();

        private readonly Dictionary<CardModel, Obj_Card> _views = new();

        // 手牌卡 → 悬停能力缓存：取出时登记、回收时注销，池复用由下次登记覆盖写
        private readonly Dictionary<Obj_Card, Comp_CardHover> _hovers = new();

        // 手牌卡 → 拖拽能力缓存：与悬停能力对称，抓取裁决按它挑出"指针下最上层且真能被拖的那张"
        private readonly Dictionary<Obj_Card, Comp_CardDrag> _drags = new();
        private readonly HashSet<Obj_Card> _entering = new();

        public HandCardRegistry(Obj_Card cardPrefab_, Transform parent_, Obj_HandView host_)
        {
            _host = host_;
            _cardPool = new ObjectPool<Obj_Card>(
                () => Object.Instantiate(cardPrefab_, parent_),
                view_ => view_.gameObject.SetActive(true),
                view_ =>
                {
                    view_.KillMotion();
                    view_.gameObject.SetActive(false);
                    _hovers.Remove(view_); // 回收即注销悬停与拖拽缓存、入场标记，防实例复用后残留旧状态
                    _drags.Remove(view_);
                    _entering.Remove(view_);
                }, defaultCapacity: 10, maxSize: 32);
        }

        /// <summary>手牌列（按落位顺序）：排布求解与逐帧裁决都按它遍历，只读不直接增删。</summary>
        public IReadOnlyList<Obj_Card> Hand => _hand;

        /// <summary>手牌张数：排布求解与"第一张恒居中"的锚定都用它。</summary>
        public int Count => _hand.Count;

        /// <summary>手牌里还有牌在抽牌入场补间中：抓取闸门按它决定"牌全部抽完落稳之前整手不许拖"。</summary>
        public bool HasEntering => _entering.Count > 0;

        // 取出并登记：取池 → 绑卡 → 接入宿主 → 进手牌列 → 标记入场中 → 认领悬停与拖拽能力
        // 能力只在这里各查一次组件，未配的卡不登记、不参与对应裁决
        public Obj_Card Spawn(CardModel model_)
        {
            Obj_Card view = _cardPool.Get();
            view.Bind(model_);
            view.SetHost(_host);
            _views[model_] = view;
            _hand.Add(view);
            _entering.Add(view);
            if (view.TryGetComponent(out Comp_CardHover hover)) _hovers[view] = hover;
            if (view.TryGetComponent(out Comp_CardDrag drag)) _drags[view] = drag;
            return view;
        }

        // 离手：只从手牌列除名，模型索引仍保留——打出后还要按模型把它找回来等到达
        public void RemoveFromHand(Obj_Card card_) => _hand.Remove(card_);

        // 归宿收尾：注销模型索引并归还卡池（离场补间已由调用方等完，回收回调会清干净补间）
        public void Release(CardModel model_, Obj_Card view_)
        {
            _views.Remove(model_);
            _cardPool.Release(view_);
        }

        public bool TryGetView(CardModel model_, out Obj_Card view_) => _views.TryGetValue(model_, out view_);

        public bool Contains(Obj_Card card_) => _hand.Contains(card_);

        public bool Contains(CardModel model_) => _views.ContainsKey(model_);

        public int IndexOf(Obj_Card card_) => _hand.IndexOf(card_);

        public bool TryGetHover(Obj_Card card_, out Comp_CardHover hover_) => _hovers.TryGetValue(card_, out hover_);

        public bool TryGetDrag(Obj_Card card_, out Comp_CardDrag drag_) => _drags.TryGetValue(card_, out drag_);

        // 指针命中的最上层手牌：命中测试与层序比较只此一处，悬停裁决与抓取裁决同源——重叠时取层序最高者（视觉最上层）
        public Obj_Card ResolveTopCard(Vector3 point_, Predicate<Obj_Card> eligible_)
        {
            Obj_Card best = null;
            foreach (Obj_Card card in _hand)
            {
                if (!eligible_(card) || !card.ContainsPoint(point_)) continue;
                if (best && card.Order <= best.Order) continue;
                best = card;
            }

            return best;
        }

        public bool IsEntering(Obj_Card card_) => _entering.Contains(card_);

        // 入场中标记只由抽牌入场演出收起（含中途离场的收尾），期间该牌不响应指针
        public void UnmarkEntering(Obj_Card card_) => _entering.Remove(card_);

        // 按当前可打状态刷整手牌外观：不可打时整卡压暗，可打时恢复本色
        public void RefreshPlayableLook(bool playable_)
        {
            foreach (Obj_Card card in _hand) card.SetPlayableLook(playable_);
        }
    }
}