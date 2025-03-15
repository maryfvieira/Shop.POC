using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Inventory.Application.Interfaces;
using Inventory.Domain.Events;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Inventory.Infrastructure.Messaging;

public class EventBus : IEventBus
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IAmazonEventBridge _eventBridgeClient;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    public EventBus(IAmazonSQS sqsClient, IAmazonEventBridge eventBridgeClient)
    {
        _sqsClient = sqsClient;
        _eventBridgeClient = eventBridgeClient;

        // Configuração do Circuit Breaker
        _circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3, // Número de falhas antes de abrir o circuito
                durationOfBreak: TimeSpan.FromSeconds(30) // Tempo de espera antes de tentar novamente
            );

        // Configuração do Retry
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task PublishAsync(IDomainEvent domainEvent)
    {
        await _circuitBreakerPolicy.ExecuteAsync(async () =>
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var eventMessage = new EventBridgeMessage
                {
                    Source = "InventoryService",
                    DetailType = domainEvent.GetType().Name,
                    Detail = JsonConvert.SerializeObject(domainEvent),
                    EventBusName = "default"
                };

                await _eventBridgeClient.PutEventsAsync(new PutEventsRequest
                {
                    Entries = new List<PutEventsRequestEntry> { eventMessage }
                });
            });
        });
    }

    public async Task<IEnumerable<IDomainEvent>> ConsumeAsync(CancellationToken cancellationToken)
    {
        return await _circuitBreakerPolicy.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/InventoryQueue",
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5,
                    AttributeNames = new List<string> { "ApproximateReceiveCount" }
                }, cancellationToken);

                var domainEvents = new List<IDomainEvent>();
                foreach (var message in receiveMessageResponse.Messages)
                {
                    try
                    {
                        var domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(message.Body);
                        domainEvents.Add(domainEvent);

                        await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                        {
                            QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/InventoryQueue",
                            ReceiptHandle = message.ReceiptHandle
                        }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        var receiveCount = int.Parse(message.Attributes["ApproximateReceiveCount"]);
                        if (receiveCount >= 3)
                        {
                            await _sqsClient.SendMessageAsync(new SendMessageRequest
                            {
                                QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/InventoryQueueDLQ",
                                MessageBody = message.Body
                            }, cancellationToken);

                            await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                            {
                                QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/InventoryQueue",
                                ReceiptHandle = message.ReceiptHandle
                            }, cancellationToken);
                        }
                        else
                        {
                            await _sqsClient.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
                            {
                                QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/InventoryQueue",
                                ReceiptHandle = message.ReceiptHandle,
                                VisibilityTimeout = 0
                            }, cancellationToken);
                        }
                    }
                }

                return domainEvents;
            });
        });
    }
}


