using Inventory.Application.Commands;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;

namespace Inventory.Application.Handlers;

public class CreateInventoryItemCommandHandler : ICommandHandler<CreateInventoryItemCommand>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IEventBus _eventBus;

    public CreateInventoryItemCommandHandler(IInventoryRepository inventoryRepository, IEventBus eventBus)
    {
        _inventoryRepository = inventoryRepository;
        _eventBus = eventBus;
    }

    public async Task Handle(CreateInventoryItemCommand command)
    {
        var inventoryItem = new InventoryItem(command.ItemId, command.Name, command.Quantity);
        await _inventoryRepository.AddAsync(inventoryItem);

        foreach (var domainEvent in inventoryItem.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent);
        }

        inventoryItem.ClearDomainEvents();
    }
}

