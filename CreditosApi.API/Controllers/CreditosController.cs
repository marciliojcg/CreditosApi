using CreditosApi.Application.DTOs;
using CreditosApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CreditosApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CreditosController : ControllerBase
{
    private readonly ICreditoService _creditoService;

    public CreditosController(ICreditoService creditoService)
    {
        _creditoService = creditoService;
    }

    [HttpPost("integrar-credito-constituido")]
    public async Task<IActionResult> IntegrarCreditos([FromBody] List<CreditoDTO> creditos)
    {
        var result = await _creditoService.IntegrarCreditosAsync(creditos);

        if (result.Success)
            return Accepted(result);

        return BadRequest(result);
    }

    [HttpGet("{numeroNfse}")]
    public async Task<IActionResult> GetCreditosPorNfse(string numeroNfse)
    {
        var creditos = await _creditoService.ObterCreditosPorNfseAsync(numeroNfse);

        if (creditos == null || creditos.Count == 0)
            return NotFound();

        return Ok(creditos);
    }

    [HttpGet("credito/{numeroCredito}")]
    public async Task<IActionResult> GetCreditoPorNumero(string numeroCredito)
    {
        var credito = await _creditoService.ObterCreditoPorNumeroAsync(numeroCredito);

        if (credito == null)
            return NotFound();

        return Ok(credito);
    }
}