namespace Inventory.Domain.Events;

public class InventoryItemUpdatedEvent : IDomainEvent
{
    public Guid ItemId { get; }
    public int NewQuantity { get; }
    public DateTime OccurredOn { get; }

    public InventoryItemUpdatedEvent(Guid itemId, int newQuantity)
    {
        ItemId = itemId;
        NewQuantity = newQuantity;
        OccurredOn = DateTime.UtcNow;
    }
}
