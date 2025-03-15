using Inventory.Application.Interfaces;

namespace Inventory.Domain.Events;

public interface IDomainEvent : IEvent
{
    DateTime OccurredOn { get; }
}
