using Confluent.Kafka;
using CreditosApi.Application.Interfaces.Messaging;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CreditosApi.Infrastructure.Messaging;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        _producer = CreateProducer();
    }

    // Construtor protegido para testes
    protected KafkaProducer(ILogger<KafkaProducer> logger, IProducer<Null, string> producer)
    {
        _logger = logger;
        _producer = producer;
    }

    private IProducer<Null, string> CreateProducer()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = "localhost:9092"
        };

        return new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, string message)
    {
        try
        {
            var result = await _producer.ProduceAsync(topic,
                new Message<Null, string> { Value = message });

            _logger.LogInformation($"Message delivered to {result.TopicPartitionOffset}");
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError($"Delivery failed: {ex.Error.Reason}");
            throw;
        }
    }

    public async Task ProduceAsync<T>(string topic, T message)
    {
        var jsonMessage = JsonSerializer.Serialize(message);
        await ProduceAsync(topic, jsonMessage);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}