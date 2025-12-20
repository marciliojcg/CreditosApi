using CreditosApi.Application.DTOs;
using CreditosApi.Application.Interfaces;
using CreditosApi.Application.Interfaces.Messaging;
using CreditosApi.Domain.Entities;
using CreditosApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CreditosApi.Application.Services;

public class CreditoService : ICreditoService
{
    private readonly ICreditoRepository _creditoRepository;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<CreditoService> _logger;

    public CreditoService(
        ICreditoRepository creditoRepository,
        IKafkaProducer kafkaProducer,
        ILogger<CreditoService> logger)
    {
        _creditoRepository = creditoRepository;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }


    public async Task<IntegrarCreditosResponse> IntegrarCreditosAsync(List<CreditoDTO> creditos)
    {
        var failures = new List<string>();

        foreach (var credito in creditos)
        {
            // Cria entidade a ser persistida
            var entity = MapToEntity(credito);

            try
            {
                // Persiste o agregado (pode ser uma operação que salva imediatamente ou apenas adiciona ao contexto)
                var persisted = await _creditoRepository.AddAsync(entity);

                // Constrói evento a ser enviado a Kafka
                var creditoEvent = new Domain.Events.CreditoRecebidoEvent
                {
                    NumeroCredito = credito.NumeroCredito,
                    NumeroNfse = credito.NumeroNfse,
                    DataConstituicao = credito.DataConstituicao,
                    ValorIssqn = credito.ValorIssqn,
                    TipoCredito = credito.TipoCredito,
                    SimplesNacional = credito.SimplesNacional,
                    Aliquota = credito.Aliquota,
                    ValorFaturado = credito.ValorFaturado,
                    ValorDeducao = credito.ValorDeducao,
                    BaseCalculo = credito.BaseCalculo
                };

                try
                {
                    // Tenta publicar o evento
                    await _kafkaProducer.ProduceAsync("integrar-credito-constituido-entry", creditoEvent);
                }
                catch (Exception produceEx)
                {
                    // Em caso de falha no envio, tenta compensar removendo o registro persistido
                    _logger.LogError(produceEx, "Falha ao publicar evento Kafka para crédito {NumeroCredito}. Tentando compensar a persistência.", persisted?.NumeroCredito ?? credito.NumeroCredito);

                    try
                    {
                        if (persisted != null)
                            await _creditoRepository.DeleteAsync(persisted);
                    }
                    catch (Exception compensacaoEx)
                    {
                        // Se a compensação falhar, registra e marca como falha permanente para investigação
                        _logger.LogError(compensacaoEx, "Falha ao compensar exclusão do crédito {NumeroCredito} após falha no envio do evento.", persisted?.NumeroCredito ?? credito.NumeroCredito);
                    }

                    failures.Add(credito.NumeroCredito);
                }
            }
            catch (Exception repoEx)
            {
                // Falha ao persistir -> registra e continua com os próximos
                _logger.LogError(repoEx, "Erro ao persistir crédito {NumeroCredito}.", credito.NumeroCredito);
                failures.Add(credito.NumeroCredito);
            }
        }

        if (!failures.Any())
        {
            return new IntegrarCreditosResponse
            {
                Success = true,
                Message = "Créditos enviados para processamento"
            };
        }

        return new IntegrarCreditosResponse
        {
            Success = false,
            Message = $"Alguns créditos falharam ao serem integrados: {string.Join(", ", failures)}"
        };
    }

    private Credito MapToEntity(CreditoDTO credito)
    {
        return new Credito
        (
            numeroCredito: credito.NumeroCredito,
            numeroNfse: credito.NumeroNfse,
            dataConstituicao:  credito.DataConstituicao,
            valorIssqn: credito.ValorIssqn,
            tipoCredito: credito.TipoCredito,
            simplesNacional: credito.SimplesNacional,
            aliquota: credito.Aliquota,
            valorFaturado: credito.ValorFaturado,
            valorDeducao: credito.ValorDeducao,
            baseCalculo: credito.BaseCalculo
       );
    }

    public async Task<CreditoDTO?> ObterCreditoPorNumeroAsync(string numeroCredito)
    {
        var credito = await _creditoRepository.GetByNumeroCreditoAsync(numeroCredito);

        if (credito == null)
            return null;

        return MapToDto(credito);
    }

    public async Task<List<CreditoDTO>> ObterCreditosPorNfseAsync(string numeroNfse)
    {
        var creditos = await _creditoRepository.GetByNumeroNfseAsync(numeroNfse);
        return creditos.Select(MapToDto).ToList();
    }

    private CreditoDTO MapToDto(Credito credito)
    {
        return new CreditoDTO
        {
            NumeroCredito = credito.NumeroCredito,
            NumeroNfse = credito.NumeroNfse,
            DataConstituicao = credito.DataConstituicao,
            ValorIssqn = credito.ValorIssqn,
            TipoCredito = credito.TipoCredito,
            SimplesNacional = credito.SimplesNacional,
            Aliquota = credito.Aliquota,
            ValorFaturado = credito.ValorFaturado,
            ValorDeducao = credito.ValorDeducao,
            BaseCalculo = credito.BaseCalculo
        };
    }
}