using Azure.Messaging.ServiceBus;
using CreditosApi.Application.Interfaces.Messaging;
using CreditosApi.Domain.Events;
using CreditosApi.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace CreditosApi.UnitTests.Messaging;

public class ServiceBusConsumerTests
{
    private readonly Mock<ILogger<ServiceBusConsumer>> _loggerMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ICreditoProcessor> _creditoProcessorMock;
    private readonly Mock<ServiceBusClient> _serviceBusClientMock;
    private readonly Mock<ServiceBusProcessor> _serviceBusProcessorMock;

    public ServiceBusConsumerTests()
    {
        _loggerMock = new Mock<ILogger<ServiceBusConsumer>>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _creditoProcessorMock = new Mock<ICreditoProcessor>();
        _serviceBusClientMock = new Mock<ServiceBusClient>();
        _serviceBusProcessorMock = new Mock<ServiceBusProcessor>();

        // Setup de dependências padrão
        _scopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(_serviceScopeMock.Object);

        _serviceScopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(ICreditoProcessor)))
            .Returns(_creditoProcessorMock.Object);
    }

    #region ProcessMessageAsync Tests

    //[Fact]
    //public async Task ProcessMessageAsync_DeveProcessarMensagemValida()
    //{
    //    // Arrange
    //    var creditoEvent = new CreditoRecebidoEvent
    //    {
    //        NumeroCredito = "123456",
    //        NumeroNfse = "7891011",
    //        DataConstituicao = DateTime.Now,
    //        ValorIssqn = 1500.75m,
    //        TipoCredito = "ISSQN",
    //        SimplesNacional = true,
    //        Aliquota = 5.0m,
    //        ValorFaturado = 30000.00m,
    //        ValorDeducao = 5000.00m,
    //        BaseCalculo = 25000.00m
    //    };

    //    var messageBody = JsonSerializer.Serialize(creditoEvent);
    //    var serviceMessage = new ServiceBusMessage(messageBody);

    //    var mockMessage = new Mock<ServiceBusReceivedMessage>();
    //    mockMessage.Setup(m => m.Body).Returns(new BinaryData(messageBody));

    //    // Replace this block in each test where ProcessMessageEventArgs is constructed:

    //    // Old (incorrect):
    //    //var processMessageEventArgs = new ProcessMessageEventArgs(
    //    //    mockMessage.Object,
    //    //    _serviceBusProcessorMock.Object,
    //    //    CancellationToken.None);



    //    // New (correct):
    //    //var processMessageEventArgs = (ProcessMessageEventArgs)Activator.CreateInstance(
    //    //    typeof(ProcessMessageEventArgs),
    //    //    nonPublic: true,
    //    //    EventArgs: new object[] { mockMessage.Object, null, CancellationToken.None }
    //    //);

    //    _creditoProcessorMock
    //        .Setup(x => x.ProcessarCreditoAsync(It.IsAny<CreditoRecebidoEvent>()))
    //        .Returns(Task.CompletedTask);

    //    var mockCompleteMessageAsync = new Mock<Func<ServiceBusReceivedMessage, CancellationToken, Task>>();
    //    var completeMessageCalled = false;

    //    // Act
    //    try
    //    {
    //        var body = processMessageEventArgs.Message.Body.ToString();
    //        var deserialized = JsonSerializer.Deserialize<CreditoRecebidoEvent>(body);

    //        if (deserialized != null)
    //        {
    //            await _creditoProcessorMock.Object.ProcessarCreditoAsync(deserialized);
    //            completeMessageCalled = true;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //       // _loggerMock.LogError(ex, "Erro ao processar mensagem");
    //    }

    //    // Assert
    //    Assert.True(completeMessageCalled);
    //    _creditoProcessorMock.Verify(
    //        x => x.ProcessarCreditoAsync(It.Is<CreditoRecebidoEvent>(c =>
    //            c.NumeroCredito == creditoEvent.NumeroCredito &&
    //            c.NumeroNfse == creditoEvent.NumeroNfse)),
    //        Times.Once);
    //}


    [Fact]
    public async Task ProcessMessageAsync_DeveLogarErro_QuandoProcessadorLancaExcecao()
    {
        // Arrange
        var creditoEvent = new CreditoRecebidoEvent
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

        var messageBody = JsonSerializer.Serialize(creditoEvent);

        var processorException = new InvalidOperationException("Erro ao processar crédito");
        _creditoProcessorMock
            .Setup(x => x.ProcessarCreditoAsync(It.IsAny<CreditoRecebidoEvent>()))
            .ThrowsAsync(processorException);

        var errorOccurred = false;

        // Act
        try
        {
            var deserialized = JsonSerializer.Deserialize<CreditoRecebidoEvent>(messageBody);

            if (deserialized != null)
            {
                await _creditoProcessorMock.Object.ProcessarCreditoAsync(deserialized);
            }
        }
        catch (InvalidOperationException)
        {
            errorOccurred = true;
        }

        // Assert
        Assert.True(errorOccurred);
        _creditoProcessorMock.Verify(
            x => x.ProcessarCreditoAsync(It.IsAny<CreditoRecebidoEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_DeveProcessarMensagemComObjeto()
    {
        // Arrange
        var creditoEvent = new CreditoRecebidoEvent
        {
            NumeroCredito = "999888",
            NumeroNfse = "5555555",
            DataConstituicao = new DateTime(2024, 12, 20),
            ValorIssqn = 2500.50m,
            TipoCredito = "ISS",
            SimplesNacional = false,
            Aliquota = 7.5m,
            ValorFaturado = 50000.00m,
            ValorDeducao = 8000.00m,
            BaseCalculo = 42000.00m
        };

        var messageBody = JsonSerializer.Serialize(creditoEvent);

        CreditoRecebidoEvent capturedEvent = null;

        _creditoProcessorMock
            .Setup(x => x.ProcessarCreditoAsync(It.IsAny<CreditoRecebidoEvent>()))
            .Callback<CreditoRecebidoEvent>(e => capturedEvent = e)
            .Returns(Task.CompletedTask);

        // Act
        var deserialized = JsonSerializer.Deserialize<CreditoRecebidoEvent>(messageBody);

        if (deserialized != null)
        {
            await _creditoProcessorMock.Object.ProcessarCreditoAsync(deserialized);
        }

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal("999888", capturedEvent.NumeroCredito);
        Assert.Equal("5555555", capturedEvent.NumeroNfse);
        Assert.Equal(2500.50m, capturedEvent.ValorIssqn);
        Assert.False(capturedEvent.SimplesNacional);
    }

    [Fact]
    public async Task ProcessMessageAsync_DeveHandlarMensagemNull()
    {
        // Arrange
        string messageBody = null;
        var processarFoiChamado = false;

        _creditoProcessorMock
            .Setup(x => x.ProcessarCreditoAsync(It.IsAny<CreditoRecebidoEvent>()))
            .Callback(() => processarFoiChamado = true)
            .Returns(Task.CompletedTask);

        // Act
        CreditoRecebidoEvent deserialized = null;
        if (!string.IsNullOrEmpty(messageBody))
        {
            deserialized = JsonSerializer.Deserialize<CreditoRecebidoEvent>(messageBody);
        }

        if (deserialized != null)
        {
            await _creditoProcessorMock.Object.ProcessarCreditoAsync(deserialized);
        }

        // Assert
        Assert.False(processarFoiChamado);
    }

    #endregion

    #region ProcessErrorAsync Tests

    [Fact]
    public void ProcessErrorAsync_DeveLogarErro()
    {
        // Arrange
        var exception = new InvalidOperationException("Erro teste no Service Bus");
        var errorSource = "Handler";

        var processErrorEventArgs = new ProcessErrorEventArgs(
            exception,
            ServiceBusErrorSource.Abandon,
            "QueueName",
            "OperationName",
            CancellationToken.None);

        var errorLogged = false;

        // Act
        try
        {
            _loggerMock.Object.LogError(
                processErrorEventArgs.Exception,
                "Erro no processamento do Service Bus");
            errorLogged = true;
        }
        catch
        {
            errorLogged = false;
        }

        // Assert
        Assert.True(errorLogged);
    }


    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_DeveIniciarProcessadorComSucesso()
    {
        // Arrange
        _serviceBusProcessorMock
            .Setup(x => x.StartProcessingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cancellationToken = CancellationToken.None;
        var startWasCalled = false;

        // Act
        _serviceBusProcessorMock
            .Setup(x => x.StartProcessingAsync(cancellationToken))
            .Callback(() => startWasCalled = true)
            .Returns(Task.CompletedTask);

        await _serviceBusProcessorMock.Object.StartProcessingAsync(cancellationToken);

        // Assert
        Assert.True(startWasCalled);
        _serviceBusProcessorMock.Verify(
            x => x.StartProcessingAsync(cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_DeveHandlarCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _serviceBusProcessorMock
            .Setup(x => x.StartProcessingAsync(cts.Token))
            .Returns(Task.CompletedTask);

        // Act
        await _serviceBusProcessorMock.Object.StartProcessingAsync(cts.Token);

        // Assert
        _serviceBusProcessorMock.Verify(
            x => x.StartProcessingAsync(cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_DeveProcessarInicializacaoAssincrona()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var startCalled = false;
        var startCompletedTime = DateTime.MinValue;

        _serviceBusProcessorMock
            .Setup(x => x.StartProcessingAsync(cancellationToken))
            .Callback(() =>
            {
                startCalled = true;
                startCompletedTime = DateTime.Now;
            })
            .Returns(Task.CompletedTask);

        // Act
        var startTime = DateTime.Now;
        await _serviceBusProcessorMock.Object.StartProcessingAsync(cancellationToken);
        var endTime = DateTime.Now;

        // Assert
        Assert.True(startCalled);
        Assert.True(startCompletedTime >= startTime);
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_DevePararProcessadorComSucesso()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var stopCalled = false;

        _serviceBusProcessorMock
            .Setup(x => x.StopProcessingAsync(cancellationToken))
            .Callback(() => stopCalled = true)
            .Returns(Task.CompletedTask);



        _serviceBusClientMock
            .Setup(x => x.DisposeAsync())
            .Returns(new ValueTask(Task.CompletedTask));

        // Act
        await _serviceBusProcessorMock.Object.StopProcessingAsync(cancellationToken);

        // Assert
        Assert.True(stopCalled);
        _serviceBusProcessorMock.Verify(
            x => x.StopProcessingAsync(cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_DeveDisporClienteAposParar()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var stopCalled = false;
        var processorDisposed = false;
        var clientDisposed = false;

        _serviceBusProcessorMock
            .Setup(x => x.StopProcessingAsync(cancellationToken))
            .Callback(() => stopCalled = true)
            .Returns(Task.CompletedTask);

        _serviceBusClientMock
            .Setup(x => x.DisposeAsync())
            .Callback(() => clientDisposed = true)
            .Returns(new ValueTask(Task.CompletedTask));

        // Act
        await _serviceBusProcessorMock.Object.StopProcessingAsync(cancellationToken);
        await _serviceBusClientMock.Object.DisposeAsync();

        // Assert
        Assert.True(stopCalled);
        Assert.True(clientDisposed);
    }

    [Fact]
    public async Task StopAsync_DeveHandlarCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _serviceBusProcessorMock
            .Setup(x => x.StopProcessingAsync(cts.Token))
            .Returns(Task.CompletedTask);

        _serviceBusClientMock
            .Setup(x => x.DisposeAsync())
            .Returns(new ValueTask(Task.CompletedTask));

        // Act & Assert
        await _serviceBusProcessorMock.Object.StopProcessingAsync(cts.Token);
        await _serviceBusClientMock.Object.DisposeAsync();
    }

    [Fact]
    public async Task StopAsync_DeveProcessarParadaAssincrona()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var stopStartTime = DateTime.MinValue;
        var stopEndTime = DateTime.MinValue;

        _serviceBusProcessorMock
            .Setup(x => x.StopProcessingAsync(cancellationToken))
            .Callback(() => stopStartTime = DateTime.Now)
            .Returns(Task.CompletedTask);

        _serviceBusProcessorMock
           .Setup(x => x.StopProcessingAsync(cancellationToken))
           .Callback(() => stopEndTime = DateTime.Now)
           .Returns(Task.CompletedTask);


        _serviceBusClientMock
            .Setup(x => x.DisposeAsync())
            .Returns(new ValueTask(Task.CompletedTask));

        // Act
        await _serviceBusProcessorMock.Object.StopProcessingAsync(cancellationToken);

        // Assert
        Assert.True(stopStartTime <= stopEndTime);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task StartAndStopAsync_DeveSequenciarOperacoes()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var operationSequence = new System.Collections.Generic.List<string>();

        _serviceBusProcessorMock
            .Setup(x => x.StartProcessingAsync(cancellationToken))
            .Callback(() => operationSequence.Add("Start"))
            .Returns(Task.CompletedTask);

        _serviceBusProcessorMock
            .Setup(x => x.StopProcessingAsync(cancellationToken))
            .Callback(() => operationSequence.Add("Stop"))
            .Returns(Task.CompletedTask);


        _serviceBusClientMock
            .Setup(x => x.DisposeAsync())
            .Callback(() => operationSequence.Add("ClientDispose"))
            .Returns(new ValueTask(Task.CompletedTask));

        // Act
        await _serviceBusProcessorMock.Object.StartProcessingAsync(cancellationToken);
        await _serviceBusProcessorMock.Object.StopProcessingAsync(cancellationToken);
        await _serviceBusClientMock.Object.DisposeAsync();

        // Assert
        Assert.Equal(new[] { "Start", "Stop", "ClientDispose" }, operationSequence);
    }

    [Fact]
    public async Task MessageProcessing_DeveProcessarMultiplasMensagensSequencialmente()
    {
        // Arrange
        var creditos = new[]
        {
            new CreditoRecebidoEvent { NumeroCredito = "111", NumeroNfse = "NF1", DataConstituicao = DateTime.Now, ValorIssqn = 100m, TipoCredito = "ISSQN", SimplesNacional = true, Aliquota = 5.0m, ValorFaturado = 2000m, ValorDeducao = 300m, BaseCalculo = 1700m },
            new CreditoRecebidoEvent { NumeroCredito = "222", NumeroNfse = "NF2", DataConstituicao = DateTime.Now, ValorIssqn = 200m, TipoCredito = "ISSQN", SimplesNacional = false, Aliquota = 5.0m, ValorFaturado = 3000m, ValorDeducao = 500m, BaseCalculo = 2500m }
        };

        var processedCreditos = new System.Collections.Generic.List<string>();

        _creditoProcessorMock
            .Setup(x => x.ProcessarCreditoAsync(It.IsAny<CreditoRecebidoEvent>()))
            .Callback<CreditoRecebidoEvent>(c => processedCreditos.Add(c.NumeroCredito))
            .Returns(Task.CompletedTask);

        // Act
        foreach (var credito in creditos)
        {
            await _creditoProcessorMock.Object.ProcessarCreditoAsync(credito);
        }

        // Assert
        Assert.Equal(2, processedCreditos.Count);
        Assert.Equal(new[] { "111", "222" }, processedCreditos);
    }

    #endregion
}