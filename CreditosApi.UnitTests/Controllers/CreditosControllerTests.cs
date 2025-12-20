
using CreditosApi.API.Controllers;
using CreditosApi.Application.DTOs;
using CreditosApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CreditosApi.UnitTests.Controllers
{
    public class CreditosControllerTests
    {
        private readonly Mock<ICreditoService> _creditoServiceMock;

        public CreditosControllerTests()
        {
            _creditoServiceMock = new Mock<ICreditoService>();
        }

        [Fact]
        public async Task IntegrarCreditos_ReturnsAccepted_WhenServiceReturnsSuccess()
        {
            // Arrange
            var response = new IntegrarCreditosResponse { Success = true, Message = "ok" };
            _creditoServiceMock
                .Setup(s => s.IntegrarCreditosAsync(It.IsAny<List<CreditoDTO>>()))
                .ReturnsAsync(response);

            var controller = new CreditosController(_creditoServiceMock.Object);
            var payload = new List<CreditoDTO> { new CreditoDTO { NumeroCredito = "1", NumeroNfse = "nfse" } };

            // Act
            var actionResult = await controller.IntegrarCreditos(payload);

            // Assert
            var accepted = Assert.IsType<AcceptedResult>(actionResult);
            Assert.Equal(response, accepted.Value);
            _creditoServiceMock.Verify(s => s.IntegrarCreditosAsync(It.IsAny<List<CreditoDTO>>()), Times.Once);
        }

        [Fact]
        public async Task IntegrarCreditos_ReturnsBadRequest_WhenServiceReturnsFailure()
        {
            // Arrange
            var response = new IntegrarCreditosResponse { Success = false, Message = "erro" };
            _creditoServiceMock
                .Setup(s => s.IntegrarCreditosAsync(It.IsAny<List<CreditoDTO>>()))
                .ReturnsAsync(response);

            var controller = new CreditosController(_creditoServiceMock.Object);
            var payload = new List<CreditoDTO> { new CreditoDTO { NumeroCredito = "1", NumeroNfse = "nfse" } };

            // Act
            var actionResult = await controller.IntegrarCreditos(payload);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Equal(response, badRequest.Value);
            _creditoServiceMock.Verify(s => s.IntegrarCreditosAsync(It.IsAny<List<CreditoDTO>>()), Times.Once);
        }

        [Fact]
        public async Task GetCreditosPorNfse_ReturnsOk_WhenServiceReturnsList()
        {
            // Arrange
            var list = new List<CreditoDTO>
            {
                new CreditoDTO { NumeroCredito = "1", NumeroNfse = "NFSE1" }
            };

            _creditoServiceMock
                .Setup(s => s.ObterCreditosPorNfseAsync("NFSE1"))
                .ReturnsAsync(list);

            var controller = new CreditosController(_creditoServiceMock.Object);

            // Act
            var actionResult = await controller.GetCreditosPorNfse("NFSE1");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal(list, ok.Value);
            _creditoServiceMock.Verify(s => s.ObterCreditosPorNfseAsync("NFSE1"), Times.Once);
        }

        [Fact]
        public async Task GetCreditosPorNfse_ReturnsNotFound_WhenServiceReturnsNullOrEmpty()
        {
            // Arrange: null
            _creditoServiceMock
                .Setup(s => s.ObterCreditosPorNfseAsync("X"))
                .ReturnsAsync((List<CreditoDTO>?)null);

            var controller = new CreditosController(_creditoServiceMock.Object);

            // Act & Assert: null
            var resultNull = await controller.GetCreditosPorNfse("X");
            Assert.IsType<NotFoundResult>(resultNull);
            _creditoServiceMock.Verify(s => s.ObterCreditosPorNfseAsync("X"), Times.Once);

            // Arrange: empty
            _creditoServiceMock
                .Setup(s => s.ObterCreditosPorNfseAsync("Y"))
                .ReturnsAsync(new List<CreditoDTO>());

            // Act & Assert: empty
            var resultEmpty = await controller.GetCreditosPorNfse("Y");
            Assert.IsType<NotFoundResult>(resultEmpty);
            _creditoServiceMock.Verify(s => s.ObterCreditosPorNfseAsync("Y"), Times.Once);
        }

        [Fact]
        public async Task GetCreditoPorNumero_ReturnsOk_WhenServiceReturnsCredito()
        {
            // Arrange
            var dto = new CreditoDTO { NumeroCredito = "123", NumeroNfse = "NFSE" };
            _creditoServiceMock
                .Setup(s => s.ObterCreditoPorNumeroAsync("123"))
                .ReturnsAsync(dto);

            var controller = new CreditosController(_creditoServiceMock.Object);

            // Act
            var actionResult = await controller.GetCreditoPorNumero("123");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal(dto, ok.Value);
            _creditoServiceMock.Verify(s => s.ObterCreditoPorNumeroAsync("123"), Times.Once);
        }

        [Fact]
        public async Task GetCreditoPorNumero_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            _creditoServiceMock
                .Setup(s => s.ObterCreditoPorNumeroAsync("999"))
                .ReturnsAsync((CreditoDTO?)null);

            var controller = new CreditosController(_creditoServiceMock.Object);

            // Act
            var actionResult = await controller.GetCreditoPorNumero("999");

            // Assert
            Assert.IsType<NotFoundResult>(actionResult);
            _creditoServiceMock.Verify(s => s.ObterCreditoPorNumeroAsync("999"), Times.Once);
        }
    }
}