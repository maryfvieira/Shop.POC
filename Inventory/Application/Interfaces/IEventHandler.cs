namespace Inventory.Application.Interfaces;

public interface IEventHandler<TEvent> where TEvent : IEvent
{
    Task Handle(TEvent domainEvent);
}
