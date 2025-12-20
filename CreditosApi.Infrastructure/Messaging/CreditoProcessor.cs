using CreditosApi.Application.Interfaces.Messaging;
using CreditosApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;


namespace CreditosApi.Infrastructure.Messaging;

public class CreditoProcessor : ICreditoProcessor
{
    private readonly ICreditoRepository _creditoRepository;
    private readonly ILogger<CreditoProcessor> _logger;

    public CreditoProcessor(
        ICreditoRepository creditoRepository,
        ILogger<CreditoProcessor> logger)
    {
        _creditoRepository = creditoRepository;
        _logger = logger;
    }

    public async Task ProcessarCreditoAsync(Domain.Events.CreditoRecebidoEvent creditoEvent)
    {
        try
        {
            // Verifica se já existe
            var existe = await _creditoRepository.ExistsByNumeroCreditoAsync(creditoEvent.NumeroCredito);

            if (!existe)
            {
                var credito = new Domain.Entities.Credito(
                    creditoEvent.NumeroCredito,
                    creditoEvent.NumeroNfse,
                    creditoEvent.DataConstituicao,
                    creditoEvent.ValorIssqn,
                    creditoEvent.TipoCredito,
                    creditoEvent.SimplesNacional,
                    creditoEvent.Aliquota,
                    creditoEvent.ValorFaturado,
                    creditoEvent.ValorDeducao,
                    creditoEvent.BaseCalculo);

                await _creditoRepository.AddAsync(credito);

                _logger.LogInformation($"Crédito {creditoEvent.NumeroCredito} inserido com sucesso");
            }
            else
            {
                _logger.LogInformation($"Crédito {creditoEvent.NumeroCredito} já existe na base");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao processar crédito {creditoEvent.NumeroCredito}");
            throw;
        }
    }
}