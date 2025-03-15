using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Amazon.SQS;
using Amazon.SQS.Model;
using Inventory.Application.Interfaces;
using Inventory.Domain.Events;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace InventoryService.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IAmazonSQS _sqsClient;
        private readonly IEventBus _eventBus;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly AsyncRetryPolicy _retryPolicy;

        public Worker(ILogger<Worker> logger, IAmazonSQS sqsClient, IEventBus eventBus)
        {
            _logger = logger;
            _sqsClient = sqsClient;
            _eventBus = eventBus;

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Usa o Circuit Breaker para consumir mensagens
                    await _circuitBreakerPolicy.ExecuteAsync(async () =>
                    {
                        // Usa o Retry para tentar consumir mensagens
                        await _retryPolicy.ExecuteAsync(async () =>
                        {
                            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                            // Consome mensagens da fila SQS
                            var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                            {
                                QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/InventoryQueue",
                                MaxNumberOfMessages = 10,
                                WaitTimeSeconds = 5,
                                AttributeNames = new List<string> { "ApproximateReceiveCount" } // Contador de tentativas
                            }, stoppingToken);

                            foreach (var message in receiveMessageResponse.Messages)
                            {
                                try
                                {
                                    // Desserializa a mensagem para um evento de domínio
                                    var domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(message.Body);

                                    // Processa o evento
                                    await _eventBus.PublishAsync(domainEvent);

                                    // Remove a mensagem da fila após o processamento bem-sucedido
                                    await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                                    {
                                        QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/InventoryQueue",
                                        ReceiptHandle = message.ReceiptHandle
                                    }, stoppingToken);

                                    _logger.LogInformation("Event processed: {eventId}", domainEvent.GetType().Name);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing message: {messageId}", message.MessageId);

                                    // Verifica o número de tentativas
                                    var receiveCount = int.Parse(message.Attributes["ApproximateReceiveCount"]);
                                    if (receiveCount >= 3) // Número máximo de tentativas
                                    {
                                        // Move a mensagem para a DLQ
                                        await _sqsClient.SendMessageAsync(new SendMessageRequest
                                        {
                                            QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/InventoryQueueDLQ",
                                            MessageBody = message.Body
                                        }, stoppingToken);

                                        // Remove a mensagem da fila original
                                        await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                                        {
                                            QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/InventoryQueue",
                                            ReceiptHandle = message.ReceiptHandle
                                        }, stoppingToken);

                                        _logger.LogWarning("Message moved to DLQ: {messageId}", message.MessageId);
                                    }
                                    else
                                    {
                                        // Reenvia a mensagem para a fila original (retry)
                                        await _sqsClient.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
                                        {
                                            QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/InventoryQueue",
                                            ReceiptHandle = message.ReceiptHandle,
                                            VisibilityTimeout = 0 // Torna a mensagem visível imediatamente
                                        }, stoppingToken);

                                        _logger.LogInformation("Message requeued for retry: {messageId}", message.MessageId);
                                    }
                                }
                            }
                        });
                    });
                }
                catch (BrokenCircuitException)
                {
                    _logger.LogWarning("Circuit is open. Waiting for recovery...");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Espera o circuito fechar
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in worker execution");
                }

                await Task.Delay(1000, stoppingToken); // Intervalo entre execuções
            }
        }
    }
}
