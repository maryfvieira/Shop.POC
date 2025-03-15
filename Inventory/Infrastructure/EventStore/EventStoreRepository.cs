using System.Data;
using Dapper;
using Inventory.Domain.Events;
using Inventory.Domain.Interfaces;
using Newtonsoft.Json;

namespace Inventory.Infrastructure.EventStore;

public class EventStoreRepository : IEventStoreRepository
{
    private readonly IDbConnection _dbConnection;

    public EventStoreRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task SaveAsync(IDomainEvent domainEvent)
    {
        var sql = "INSERT INTO EventStore (EventId, EventType, EventData, OccurredOn) VALUES (@EventId, @EventType, @EventData, @OccurredOn)";
        await _dbConnection.ExecuteAsync(sql, new
        {
            EventId = Guid.NewGuid(),
            EventType = domainEvent.GetType().Name,
            EventData = JsonConvert.SerializeObject(domainEvent),
            OccurredOn = DateTime.UtcNow
        });
    }
}
