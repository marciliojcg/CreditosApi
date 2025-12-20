using CreditosApi.Domain.Entities;
using CreditosApi.Domain.Events;
using CreditosApi.Domain.Interfaces;
using CreditosApi.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Moq;

namespace CreditosApi.UnitTests.Messaging;

public class CreditoProcessorTests
{
    private readonly Mock<ICreditoRepository> _creditoRepositoryMock;
    private readonly Mock<ILogger<CreditoProcessor>> _loggerMock;
    private readonly CreditoProcessor _processor;

    public CreditoProcessorTests()
    {
        _creditoRepositoryMock = new Mock<ICreditoRepository>();
        _loggerMock = new Mock<ILogger<CreditoProcessor>>();
        _processor = new CreditoProcessor(_creditoRepositoryMock.Object, _loggerMock.Object);
    }

    #region ProcessarCreditoAsync - Success Cases

    [Fact]
    public async Task ProcessarCreditoAsync_DeveInserirCredito_QuandoNaoExiste()
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

        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito))
            .ReturnsAsync(false);

        _creditoRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Credito>()))
            .ReturnsAsync(new Credito(
                creditoEvent.NumeroCredito,
                creditoEvent.NumeroNfse,
                creditoEvent.DataConstituicao,
                creditoEvent.ValorIssqn,
                creditoEvent.TipoCredito,
                creditoEvent.SimplesNacional,
                creditoEvent.Aliquota,
                creditoEvent.ValorFaturado,
                creditoEvent.ValorDeducao,
                creditoEvent.BaseCalculo));

        // Act
        await _processor.ProcessarCreditoAsync(creditoEvent);

        // Assert
        _creditoRepositoryMock.Verify(
            x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito),
            Times.Once);

        _creditoRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Credito>(c =>
                c.NumeroCredito == creditoEvent.NumeroCredito &&
                c.NumeroNfse == creditoEvent.NumeroNfse &&
                c.ValorIssqn == creditoEvent.ValorIssqn)),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("inserido com sucesso")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarCreditoAsync_DevePreservarTodosOsCampos_QuandoInserirCredito()
    {
        // Arrange
        var dataConstituicao = new DateTime(2024, 12, 19);
        var creditoEvent = new CreditoRecebidoEvent
        {
            NumeroCredito = "999888",
            NumeroNfse = "5555555",
            DataConstituicao = dataConstituicao,
            ValorIssqn = 2500.50m,
            TipoCredito = "ISS",
            SimplesNacional = false,
            Aliquota = 7.5m,
            ValorFaturado = 50000.00m,
            ValorDeducao = 8000.00m,
            BaseCalculo = 42000.00m
        };

        Credito creditoCapturado = null;

        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito))
            .ReturnsAsync(false);

        _creditoRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Credito>()))
            .Callback<Credito>(c => creditoCapturado = c)
            .ReturnsAsync((Credito c) => c);

        // Act
        await _processor.ProcessarCreditoAsync(creditoEvent);

        // Assert
        Assert.NotNull(creditoCapturado);
        Assert.Equal("999888", creditoCapturado.NumeroCredito);
        Assert.Equal("5555555", creditoCapturado.NumeroNfse);
        Assert.Equal(dataConstituicao, creditoCapturado.DataConstituicao);
        Assert.Equal(2500.50m, creditoCapturado.ValorIssqn);
        Assert.Equal("ISS", creditoCapturado.TipoCredito);
        Assert.False(creditoCapturado.SimplesNacional);
        Assert.Equal(7.5m, creditoCapturado.Aliquota);
        Assert.Equal(50000.00m, creditoCapturado.ValorFaturado);
        Assert.Equal(8000.00m, creditoCapturado.ValorDeducao);
        Assert.Equal(42000.00m, creditoCapturado.BaseCalculo);
    }

    [Fact]
    public async Task ProcessarCreditoAsync_NaoDeveinserirCredito_QuandoJaExiste()
    {
        // Arrange
        var creditoEvent = new CreditoRecebidoEvent
        {
            NumeroCredito = "111111",
            NumeroNfse = "2222222",
            DataConstituicao = DateTime.Now,
            ValorIssqn = 1000m,
            TipoCredito = "ISSQN",
            SimplesNacional = true,
            Aliquota = 5.0m,
            ValorFaturado = 20000m,
            ValorDeducao = 3000m,
            BaseCalculo = 17000m
        };

        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito))
            .ReturnsAsync(true);

        // Act
        await _processor.ProcessarCreditoAsync(creditoEvent);

        // Assert
        _creditoRepositoryMock.Verify(
            x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito),
            Times.Once);

        _creditoRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Credito>()),
            Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("já existe na base")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region ProcessarCreditoAsync - Exception Cases

    [Fact]
    public async Task ProcessarCreditoAsync_DeveLogarErro_QuandoExistsByNumeroCreditoLancaExcecao()
    {
        // Arrange
        var creditoEvent = new CreditoRecebidoEvent
        {
            NumeroCredito = "333333",
            NumeroNfse = "4444444",
            DataConstituicao = DateTime.Now,
            ValorIssqn = 1500m,
            TipoCredito = "ISSQN",
            SimplesNacional = true,
            Aliquota = 5.0m,
            ValorFaturado = 30000m,
            ValorDeducao = 5000m,
            BaseCalculo = 25000m
        };

        var exceptionMessage = "Erro ao verificar existência do crédito";
        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _processor.ProcessarCreditoAsync(creditoEvent));

        Assert.Equal(exceptionMessage, exception.Message);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro ao processar crédito")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarCreditoAsync_DeveLogarErro_QuandoAddAsyncLancaExcecao()
    {
        // Arrange
        var creditoEvent = new CreditoRecebidoEvent
        {
            NumeroCredito = "555555",
            NumeroNfse = "6666666",
            DataConstituicao = DateTime.Now,
            ValorIssqn = 2000m,
            TipoCredito = "ISSQN",
            SimplesNacional = false,
            Aliquota = 3.5m,
            ValorFaturado = 40000m,
            ValorDeducao = 7000m,
            BaseCalculo = 33000m
        };

        var exceptionMessage = "Erro ao persistir crédito";
        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito))
            .ReturnsAsync(false);

        _creditoRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Credito>()))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _processor.ProcessarCreditoAsync(creditoEvent));

        Assert.Equal(exceptionMessage, exception.Message);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro ao processar crédito") &&
                                              v.ToString().Contains("555555")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarCreditoAsync_DeveCapturarExcecaoGenerica()
    {
        // Arrange
        var creditoEvent = new CreditoRecebidoEvent
        {
            NumeroCredito = "777777",
            NumeroNfse = "8888888",
            DataConstituicao = DateTime.Now,
            ValorIssqn = 1200m,
            TipoCredito = "ISSQN",
            SimplesNacional = true,
            Aliquota = 5.0m,
            ValorFaturado = 24000m,
            ValorDeducao = 4000m,
            BaseCalculo = 20000m
        };

        var exceptionMessage = "Erro desconhecido do banco de dados";
        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _processor.ProcessarCreditoAsync(creditoEvent));

        Assert.Equal(exceptionMessage, exception.Message);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarCreditoAsync_DeveLogarErroComNumeroCreditoNoMensage()
    {
        // Arrange
        var creditoEvent = new CreditoRecebidoEvent
        {
            NumeroCredito = "ESPECIAL123",
            NumeroNfse = "9999999",
            DataConstituicao = DateTime.Now,
            ValorIssqn = 1500m,
            TipoCredito = "ISSQN",
            SimplesNacional = true,
            Aliquota = 5.0m,
            ValorFaturado = 30000m,
            ValorDeducao = 5000m,
            BaseCalculo = 25000m
        };

        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito))
            .ThrowsAsync(new TimeoutException("Timeout na operação"));

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(
            () => _processor.ProcessarCreditoAsync(creditoEvent));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ESPECIAL123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region Integration/Flow Tests

    [Fact]
    public async Task ProcessarCreditoAsync_DeveVerificarExistenciaAntesDeInserir()
    {
        // Arrange
        var creditoEvent = new CreditoRecebidoEvent
        {
            NumeroCredito = "FLOW001",
            NumeroNfse = "NFSEFLOW",
            DataConstituicao = DateTime.Now,
            ValorIssqn = 1000m,
            TipoCredito = "ISSQN",
            SimplesNacional = true,
            Aliquota = 5.0m,
            ValorFaturado = 20000m,
            ValorDeducao = 3000m,
            BaseCalculo = 17000m
        };

        var callOrder = 0;
        var existsCallOrder = 0;
        var addCallOrder = 0;

        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito))
            .Callback(() => existsCallOrder = ++callOrder)
            .ReturnsAsync(false);

        _creditoRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Credito>()))
            .Callback(() => addCallOrder = ++callOrder)
            .ReturnsAsync((Credito c) => c);

        // Act
        await _processor.ProcessarCreditoAsync(creditoEvent);

        // Assert
        Assert.True(existsCallOrder < addCallOrder,
            "ExistsByNumeroCreditoAsync deve ser chamado antes de AddAsync");
    }

    [Fact]
    public async Task ProcessarCreditoAsync_ComMultiplosCreditosDiferentes_DeveProcessarCadaUm()
    {
        // Arrange
        var creditos = new[]
        {
            new CreditoRecebidoEvent { NumeroCredito = "A1", NumeroNfse = "NF1", DataConstituicao = DateTime.Now, ValorIssqn = 100m, TipoCredito = "ISSQN", SimplesNacional = true, Aliquota = 5.0m, ValorFaturado = 2000m, ValorDeducao = 300m, BaseCalculo = 1700m },
            new CreditoRecebidoEvent { NumeroCredito = "A2", NumeroNfse = "NF2", DataConstituicao = DateTime.Now, ValorIssqn = 200m, TipoCredito = "ISSQN", SimplesNacional = false, Aliquota = 5.0m, ValorFaturado = 3000m, ValorDeducao = 500m, BaseCalculo = 2500m }
        };

        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _creditoRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Credito>()))
            .ReturnsAsync((Credito c) => c);

        // Act
        foreach (var credito in creditos)
        {
            await _processor.ProcessarCreditoAsync(credito);
        }

        // Assert
        _creditoRepositoryMock.Verify(
            x => x.ExistsByNumeroCreditoAsync(It.IsAny<string>()),
            Times.Exactly(2));

        _creditoRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Credito>()),
            Times.Exactly(2));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ProcessarCreditoAsync_DeveProcessarCredito_ComValoresZero()
    {
        // Arrange
        var creditoEvent = new CreditoRecebidoEvent
        {
            NumeroCredito = "ZERO001",
            NumeroNfse = "NFSEZERO",
            DataConstituicao = DateTime.Now,
            ValorIssqn = 0m,
            TipoCredito = "ISSQN",
            SimplesNacional = true,
            Aliquota = 0m,
            ValorFaturado = 0m,
            ValorDeducao = 0m,
            BaseCalculo = 0m
        };

        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito))
            .ReturnsAsync(false);

        _creditoRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Credito>()))
            .ReturnsAsync((Credito c) => c);

        // Act
        await _processor.ProcessarCreditoAsync(creditoEvent);

        // Assert
        _creditoRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Credito>(c =>
                c.NumeroCredito == "ZERO001" &&
                c.ValorIssqn == 0m)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarCreditoAsync_DeveProcessarCredito_ComValoresGrandes()
    {
        // Arrange
        var creditoEvent = new CreditoRecebidoEvent
        {
            NumeroCredito = "GRANDE001",
            NumeroNfse = "NFSEGRANDE",
            DataConstituicao = DateTime.Now,
            ValorIssqn = decimal.MaxValue / 1000,
            TipoCredito = "ISSQN",
            SimplesNacional = true,
            Aliquota = 99.99m,
            ValorFaturado = decimal.MaxValue / 1000,
            ValorDeducao = decimal.MaxValue / 1000,
            BaseCalculo = decimal.MaxValue / 1000
        };

        _creditoRepositoryMock
            .Setup(x => x.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito))
            .ReturnsAsync(false);

        _creditoRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Credito>()))
            .ReturnsAsync((Credito c) => c);

        // Act
        await _processor.ProcessarCreditoAsync(creditoEvent);

        // Assert
        _creditoRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Credito>()),
            Times.Once);
    }

    #endregion
}