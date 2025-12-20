using Confluent.Kafka;
using CreditosApi.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace CreditosApi.UnitTests.Messaging;

public class KafkaProducerTests : IDisposable
{
    private readonly Mock<ILogger<KafkaProducer>> _loggerMock;
    private readonly Mock<IProducer<Null, string>> _kafkaProducerMock;
    private KafkaProducer _kafkaProducer;

    public KafkaProducerTests()
    {
        _loggerMock = new Mock<ILogger<KafkaProducer>>();
        _kafkaProducerMock = new Mock<IProducer<Null, string>>();
    }

    #region ProduceAsync(string, string) Tests

    [Fact]
    public async Task ProduceAsync_String_DeveEnviarMensagemComSucesso()
    {
        // Arrange
        var topic = "test-topic";
        var message = "test message";

        var deliveryResult = new DeliveryResult<Null, string>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(1),
            Message = new Message<Null, string> { Value = message }
        };

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(deliveryResult);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act
        await _kafkaProducer.ProduceAsync(topic, message);

        // Assert
        _kafkaProducerMock.Verify(
            x => x.ProduceAsync(
                topic,
                It.Is<Message<Null, string>>(m => m.Value == message),
                default),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("delivered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProduceAsync_String_DeveLogarTopicPartitionOffset()
    {
        // Arrange
        var topic = "creditos-topic";
        var message = "credito message";

        var deliveryResult = new DeliveryResult<Null, string>
        {
            Topic = topic,
            Partition = new Partition(2),
            Offset = new Offset(42),
            Message = new Message<Null, string> { Value = message }
        };

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(deliveryResult);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act
        await _kafkaProducer.ProduceAsync(topic, message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().Contains(topic) &&
                    v.ToString().Contains("2") &&
                    v.ToString().Contains("42")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProduceAsync_String_DeveProcessarMensagemVazia()
    {
        // Arrange
        var topic = "test-topic";
        var message = string.Empty;

        var deliveryResult = new DeliveryResult<Null, string>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(1),
            Message = new Message<Null, string> { Value = message }
        };

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(deliveryResult);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act
        await _kafkaProducer.ProduceAsync(topic, message);

        // Assert
        _kafkaProducerMock.Verify(
            x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ProduceAsync_String_DeveProcessarMensagemGrande()
    {
        // Arrange
        var topic = "test-topic";
        var message = new string('x', 1000000); // 1MB message

        var deliveryResult = new DeliveryResult<Null, string>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(1),
            Message = new Message<Null, string> { Value = message }
        };

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(deliveryResult);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act
        await _kafkaProducer.ProduceAsync(topic, message);

        // Assert
        _kafkaProducerMock.Verify(
            x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ProduceAsync_String_DeveProcessarMensagemComCaracteresEspeciais()
    {
        // Arrange
        var topic = "test-topic";
        var message = "Message with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";

        var deliveryResult = new DeliveryResult<Null, string>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(1),
            Message = new Message<Null, string> { Value = message }
        };

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(deliveryResult);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act
        await _kafkaProducer.ProduceAsync(topic, message);

        // Assert
        _kafkaProducerMock.Verify(
            x => x.ProduceAsync(
                topic,
                It.Is<Message<Null, string>>(m => m.Value == message),
                default),
            Times.Once);
    }


    [Fact]
    public async Task ProduceAsync_String_DeveLogarErroComMotivoEspecifico()
    {
        // Arrange
        var topic = "test-topic";
        var message = "test message";
        var errorReason = "Broker: Message size is too large";

        var produceException = new ProduceException<Null, string>(
            new Error(ErrorCode.MsgSizeTooLarge, errorReason),
            null);

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ThrowsAsync(produceException);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProduceException<Null, string>>(
            () => _kafkaProducer.ProduceAsync(topic, message));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorReason)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region ProduceAsync<T>(string, T) Tests

    [Fact]
    public async Task ProduceAsync_Generic_DeveSerializarEEnviarObjeto()
    {
        // Arrange
        var topic = "credito-topic";
        var creditoObj = new { NumeroCredito = "123456", NumeroNfse = "7891011" };
        var jsonMessage = JsonSerializer.Serialize(creditoObj);

        var deliveryResult = new DeliveryResult<Null, string>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(1),
            Message = new Message<Null, string> { Value = jsonMessage }
        };

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(deliveryResult);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act
        await _kafkaProducer.ProduceAsync(topic, creditoObj);

        // Assert
        _kafkaProducerMock.Verify(
            x => x.ProduceAsync(
                topic,
                It.Is<Message<Null, string>>(m => m.Value == jsonMessage),
                default),
            Times.Once);
    }


    [Fact]
    public async Task ProduceAsync_Generic_DeveSerializarObjetoComplexo()
    {
        // Arrange
        var topic = "credito-topic";
        var creditoObj = new
        {
            NumeroCredito = "123456",
            NumeroNfse = "7891011",
            DataConstituicao = DateTime.Now,
            ValorIssqn = 1500.75m,
            TipoCredito = "ISSQN",
            SimplesNacional = true,
            Aliquota = 5.0m,
            ValorFaturado = 30000.00m,
            ValorDeducao = 5000.00m,
            BaseCalculo = 25000.00m
        };

        var jsonMessage = JsonSerializer.Serialize(creditoObj);

        var deliveryResult = new DeliveryResult<Null, string>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(1),
            Message = new Message<Null, string> { Value = jsonMessage }
        };

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(deliveryResult);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act
        await _kafkaProducer.ProduceAsync(topic, creditoObj);

        // Assert
        _kafkaProducerMock.Verify(
            x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ProduceAsync_Generic_DeveSerializarLista()
    {
        // Arrange
        var topic = "creditos-topic";
        var creditosList = new[]
        {
            new { NumeroCredito = "111", NumeroNfse = "NF1" },
            new { NumeroCredito = "222", NumeroNfse = "NF2" }
        };

        var jsonMessage = JsonSerializer.Serialize(creditosList);

        var deliveryResult = new DeliveryResult<Null, string>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(1),
            Message = new Message<Null, string> { Value = jsonMessage }
        };

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(deliveryResult);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act
        await _kafkaProducer.ProduceAsync(topic, creditosList);

        // Assert
        _kafkaProducerMock.Verify(
            x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ProduceAsync_Generic_DevePropagaProduceException()
    {
        // Arrange
        var topic = "credito-topic";
        var creditoObj = new { NumeroCredito = "123456" };

        var produceException = new ProduceException<Null, string>(
            new Error(ErrorCode.BrokerNotAvailable, "All brokers are down"),
            null);

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ThrowsAsync(produceException);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ProduceException<Null, string>>(
            () => _kafkaProducer.ProduceAsync(topic, creditoObj));
    }

    [Fact]
    public async Task ProduceAsync_Generic_DeveSerializarObjetoComValoresNull()
    {
        // Arrange
        var topic = "test-topic";
        var obj = new { Name = (string)null, Value = 0 };
        var jsonMessage = JsonSerializer.Serialize(obj);

        var deliveryResult = new DeliveryResult<Null, string>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(1),
            Message = new Message<Null, string> { Value = jsonMessage }
        };

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(deliveryResult);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act
        await _kafkaProducer.ProduceAsync(topic, obj);

        // Assert
        _kafkaProducerMock.Verify(
            x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default),
            Times.Once);
    }

    #endregion

    #region Topic Validation Tests

    [Fact]
    public async Task ProduceAsync_DeveProcessarTopicsComNomesValidos()
    {
        // Arrange
        var topics = new[] { "topic-1", "topic_2", "topic.3", "TOPIC4" };

        foreach (var topic in topics)
        {
            var deliveryResult = new DeliveryResult<Null, string>
            {
                Topic = topic,
                Partition = new Partition(0),
                Offset = new Offset(1),
                Message = new Message<Null, string> { Value = "message" }
            };

            _kafkaProducerMock
                .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
                .ReturnsAsync(deliveryResult);
        }

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act & Assert
        foreach (var topic in topics)
        {
            await _kafkaProducer.ProduceAsync(topic, "test message");

            _kafkaProducerMock.Verify(
                x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default),
                Times.Once);
        }
    }

    #endregion

    #region Multiple Messages Tests

    [Fact]
    public async Task ProduceAsync_DeveEnviarMultiplasMensagensSequencialmente()
    {
        // Arrange
        var topic = "test-topic";
        var messages = new[] { "msg1", "msg2", "msg3" };

        var deliveryResult = new DeliveryResult<Null, string>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(1),
            Message = new Message<Null, string> { Value = "" }
        };

        _kafkaProducerMock
            .Setup(x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(deliveryResult);

        _kafkaProducer = new TestableKafkaProducer(_loggerMock.Object, _kafkaProducerMock.Object);

        // Act
        foreach (var message in messages)
        {
            await _kafkaProducer.ProduceAsync(topic, message);
        }

        // Assert
        _kafkaProducerMock.Verify(
            x => x.ProduceAsync(topic, It.IsAny<Message<Null, string>>(), default),
            Times.Exactly(3));
    }

    #endregion

    public void Dispose()
    {
        _kafkaProducer?.Dispose();
    }
}

/// <summary>
/// Classe testável que permite injetar um mock do IProducer<Null, string>
/// para fins de testes unitários.
/// </summary>
internal class TestableKafkaProducer : KafkaProducer
{
    private readonly IProducer<Null, string> _mockProducer;

    public TestableKafkaProducer(
        ILogger<KafkaProducer> logger,
        IProducer<Null, string> mockProducer) : base(logger)
    {
        _mockProducer = mockProducer;
        ReplaceProducer(mockProducer);
    }

    private void ReplaceProducer(IProducer<Null, string> producer)
    {
        // Usa reflexão para atualizar o campo privado _producer
        var field = typeof(KafkaProducer).GetField("_producer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, producer);
    }
}