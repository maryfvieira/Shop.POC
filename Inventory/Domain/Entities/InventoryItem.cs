using Inventory.Domain.Events;

namespace Inventory.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public int Quantity { get; private set; }

    private List<IDomainEvent> _domainEvents = new List<IDomainEvent>();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public InventoryItem(Guid id, string name, int quantity)
    {
        Id = id;
        Name = name;
        Quantity = quantity;
        _domainEvents.Add(new InventoryItemCreatedEvent(id, name, quantity));
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity < 0)
            throw new InvalidOperationException("Quantity cannot be negative.");

        Quantity = newQuantity;
        _domainEvents.Add(new InventoryItemUpdatedEvent(Id, newQuantity));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
