using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces;

public interface IInventoryRepository
{
    Task<InventoryItem> GetByIdAsync(Guid id);
    Task AddAsync(InventoryItem item);
    Task UpdateAsync(InventoryItem item);
}
