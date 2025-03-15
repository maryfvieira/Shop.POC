namespace Inventory.Domain.Events;

public class InventoryItemCreatedEvent : IDomainEvent
{
    public Guid ItemId { get; }
    public string Name { get; }
    public int Quantity { get; }
    public DateTime OccurredOn { get; }

    public InventoryItemCreatedEvent(Guid itemId, string name, int quantity)
    {
        ItemId = itemId;
        Name = name;
        Quantity = quantity;
        OccurredOn = DateTime.UtcNow;
    }
}
