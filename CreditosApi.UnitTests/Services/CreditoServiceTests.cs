using CreditosApi.Application.DTOs;
using CreditosApi.Application.Interfaces.Messaging;
using CreditosApi.Application.Services;
using CreditosApi.Domain.Entities;
using CreditosApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace CreditosApi.UnitTests.Services
{
    public class CreditoServiceTests
    {
        private readonly Mock<ICreditoRepository> _creditoRepositoryMock;
        private readonly Mock<IKafkaProducer> _kafkaProducerMock;
        private readonly Mock<ILogger<CreditoService>> _loggerMock;

        public CreditoServiceTests()
        {
            _creditoRepositoryMock = new Mock<ICreditoRepository>();
            _kafkaProducerMock = new Mock<IKafkaProducer>();
            _loggerMock = new Mock<ILogger<CreditoService>>();
        }

        [Fact]
        public async Task IntegrarCreditosAsync_DeveEnviarParaKafka()
        {
            // Arrange
            var service = new CreditoService(_creditoRepositoryMock.Object, _kafkaProducerMock.Object, _loggerMock.Object);
            List<CreditoDTO> creditos = PopularCreditos();

            _kafkaProducerMock
                .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.IntegrarCreditosAsync(creditos);

            // Assert
            Assert.True(result.Success);
            _kafkaProducerMock.Verify(
                x => x.ProduceAsync("integrar-credito-constituido-entry", It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task IntegrarCreditosAsync_QuandoRepositorioLancaExcecao_DeveRetornarSucessoFalso()
        {
            // Arrange
            var service = new CreditoService(_creditoRepositoryMock.Object, _kafkaProducerMock.Object, _loggerMock.Object);
            var creditos = PopularCreditos();

            _creditoRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Credito>()))
                .ThrowsAsync(new InvalidOperationException("Erro ao persistir crédito"));

            // Act
            var result = await service.IntegrarCreditosAsync(creditos);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("falharam", result.Message);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro ao persistir crédito")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

       

        [Fact]
        public async Task IntegrarCreditosAsync_ComMultiplosCreditosEUmFalha_DeveRetornarErroComCreditosFalhados()
        {
            // Arrange
            var service = new CreditoService(_creditoRepositoryMock.Object, _kafkaProducerMock.Object, _loggerMock.Object);
            var creditos = new List<CreditoDTO>
            {
                new CreditoDTO
                {
                    NumeroCredito = "111111",
                    NumeroNfse = "1111111",
                    DataConstituicao = DateTime.Now,
                    ValorIssqn = 1000.00m,
                    TipoCredito = "ISSQN",
                    SimplesNacional = true,
                    Aliquota = 5.0m,
                    ValorFaturado = 20000.00m,
                    ValorDeducao = 3000.00m,
                    BaseCalculo = 17000.00m
                },
                new CreditoDTO
                {
                    NumeroCredito = "222222",
                    NumeroNfse = "2222222",
                    DataConstituicao = DateTime.Now,
                    ValorIssqn = 1500.75m,
                    TipoCredito = "ISSQN",
                    SimplesNacional = true,
                    Aliquota = 5.0m,
                    ValorFaturado = 30000.00m,
                    ValorDeducao = 5000.00m,
                    BaseCalculo = 25000.00m
                }
            };

            // Primeiro crédito sucesso, segundo falha no repositório
            _creditoRepositoryMock
                .SetupSequence(x => x.AddAsync(It.IsAny<Credito>()))
                .ReturnsAsync(PopularCredito())
                .ThrowsAsync(new InvalidOperationException("Erro na persistência"));

            _kafkaProducerMock
                .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.IntegrarCreditosAsync(creditos);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("222222", result.Message);
            _kafkaProducerMock.Verify(
                x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<object>()),
                Times.Once);
        }


      

        [Fact]
        public async Task IntegrarCreditosAsync_ComListaVazia_DeveRetornarSucesso()
        {
            // Arrange
            var service = new CreditoService(_creditoRepositoryMock.Object, _kafkaProducerMock.Object, _loggerMock.Object);
            var creditos = new List<CreditoDTO>();

            // Act
            var result = await service.IntegrarCreditosAsync(creditos);

            // Assert
            Assert.True(result.Success);
            _creditoRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Credito>()), Times.Never);
            _kafkaProducerMock.Verify(
                x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<object>()),
                Times.Never);
        }


        [Fact]
        public async Task IntegrarCreditosAsync_ComTimeoutNoKafka_DeveCapturarExcecaoEMarcarComoFalha()
        {
            // Arrange
            var service = new CreditoService(_creditoRepositoryMock.Object, _kafkaProducerMock.Object, _loggerMock.Object);
            var creditos = PopularCreditos();
            var creditoPersistido = PopularCredito();

            _creditoRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Credito>()))
                .ReturnsAsync(creditoPersistido);

            _kafkaProducerMock
                .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new TimeoutException("Timeout ao publicar em Kafka"));

            // Act
            var result = await service.IntegrarCreditosAsync(creditos);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("falharam", result.Message);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<TimeoutException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        private static List<CreditoDTO> PopularCreditos()
        {
            return new List<CreditoDTO>
            {
                new CreditoDTO
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
                }
            };
        }

        [Fact]
        public async Task ObterCreditoPorNumeroAsync_DeveRetornarCredito()
        {
            // Arrange
            var credito = new Credito(
                "123456",
                "7891011",
                DateTime.Now,
                1500.75m,
                "ISSQN",
                true,
                5.0m,
                30000.00m,
                5000.00m,
                25000.00m);

            _creditoRepositoryMock
                .Setup(x => x.GetByNumeroCreditoAsync("123456"))
                .ReturnsAsync(credito);

            var service = new CreditoService(_creditoRepositoryMock.Object, _kafkaProducerMock.Object, _loggerMock.Object);

            // Act
            var result = await service.ObterCreditoPorNumeroAsync("123456");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("123456", result.NumeroCredito);
        }



        [Fact]
        public async Task ObterCreditoPorNfseAsync_DeveRetornarCredito()
        {
            // Arrange
            Credito credito = PopularCredito();

            _creditoRepositoryMock
               .Setup(x => x.GetByNumeroNfseAsync("7891011"))
               .ReturnsAsync(new List<Credito> { credito });

            var service = new CreditoService(_creditoRepositoryMock.Object, _kafkaProducerMock.Object, _loggerMock.Object);

            // Act
            var result = await service.ObterCreditosPorNfseAsync("7891011");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Count);
        }

        private static Credito PopularCredito()
        {
            return new Credito(
                            "123456",
                            "7891011",
                            DateTime.Now,
                            1500.75m,
                            "ISSQN",
                            true,
                            5.0m,
                            30000.00m,
                            5000.00m,
                            25000.00m);
        }
    }
}