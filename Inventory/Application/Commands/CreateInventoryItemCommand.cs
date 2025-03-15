using Inventory.Application.Interfaces;

namespace Inventory.Application.Commands;

public class CreateInventoryItemCommand : ICommand
{
    public Guid ItemId { get; }
    public string Name { get; }
    public int Quantity { get; }

    public CreateInventoryItemCommand(Guid itemId, string name, int quantity)
    {
        ItemId = itemId;
        Name = name;
        Quantity = quantity;
    }
}
