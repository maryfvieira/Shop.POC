using Amazon.EventBridge.Model;

namespace Inventory.Infrastructure.Messaging;

public class EventBridgeMessage : PutEventsRequestEntry
{
    public EventBridgeMessage()
    {
        Source = "InventoryService";
        EventBusName = "default";
    }
}
