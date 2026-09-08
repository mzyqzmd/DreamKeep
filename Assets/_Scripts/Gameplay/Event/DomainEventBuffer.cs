using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>事件缓冲：结算按发生顺序只追加，表现层取走后清空；Domain 不知道有没有人在听。</summary>
    public class DomainEventBuffer
    {
        private readonly List<IDomainEvent> _events = new();

        public int Count => _events.Count;
        public IReadOnlyList<IDomainEvent> Events => _events;

        /// <summary>写入一条领域事件，顺序即发生顺序。</summary>
        public void Emit(IDomainEvent domainEvent_) => _events.Add(domainEvent_);

        /// <summary>取出全部事件并清空，无头场景直接丢弃即可。</summary>
        public List<IDomainEvent> Drain()
        {
            List<IDomainEvent> drained = new(_events);
            _events.Clear();
            return drained;
        }

        /// <summary>丢弃全部事件，用于无头模拟不关心表现的场合。</summary>
        public void Clear() => _events.Clear();
    }
}
