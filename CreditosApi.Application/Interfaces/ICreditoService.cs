using CreditosApi.Application.DTOs;

namespace CreditosApi.Application.Interfaces;

public interface ICreditoService
{
    Task<IntegrarCreditosResponse> IntegrarCreditosAsync(List<CreditoDTO> creditos);
    Task<CreditoDTO?> ObterCreditoPorNumeroAsync(string numeroCredito);
    Task<List<CreditoDTO>> ObterCreditosPorNfseAsync(string numeroNfse);
}