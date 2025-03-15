
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Amazon.SQS;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Inventory;
using Inventory.Application.Commands;
using Inventory.Application.Events;
using Inventory.Application.Handlers;
using Inventory.Application.Interfaces;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.EventStore;
using Inventory.Infrastructure.Repositories;
using InventoryService.Worker;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using EventBus = Inventory.Infrastructure.Messaging.EventBus;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Database
        services.AddScoped<IDbConnection>(_ =>
            new MySqlConnection(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IEventStoreRepository, EventStoreRepository>();

        // AWS Clients
        services.AddAWSService<IAmazonSQS>();
        services.AddAWSService<IAmazonEventBridge>();

        // Event Bus
        services.AddSingleton<IEventBus, EventBus>();

        // Handlers
        services.AddScoped<ICommandHandler<CreateInventoryItemCommand>, CreateInventoryItemCommandHandler>();
        services.AddScoped<IEventHandler<InventoryItemCreatedEvent>, InventoryItemCreatedEventHandler>();

        // Worker
        services.AddHostedService<Worker>();
    })
    .Build();

await builder.RunAsync();
