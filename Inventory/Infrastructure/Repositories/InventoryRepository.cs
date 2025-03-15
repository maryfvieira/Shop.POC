using System.Data;
using Dapper;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;

namespace Inventory.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly IDbConnection _dbConnection;

    public InventoryRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<InventoryItem> GetByIdAsync(Guid id)
    {
        var item = await _dbConnection.QueryFirstOrDefaultAsync<InventoryItem>(
            "SELECT * FROM InventoryItems WHERE Id = @Id", new { Id = id });
        return item;
    }

    public async Task AddAsync(InventoryItem item)
    {
        var sql = "INSERT INTO InventoryItems (Id, Name, Quantity) VALUES (@Id, @Name, @Quantity)";
        await _dbConnection.ExecuteAsync(sql, item);
    }

    public async Task UpdateAsync(InventoryItem item)
    {
        var sql = "UPDATE InventoryItems SET Name = @Name, Quantity = @Quantity WHERE Id = @Id";
        await _dbConnection.ExecuteAsync(sql, item);
    }
}
