namespace CreditosApi.Domain.Entities;

public class Credito
{
    public long Id { get; private set; }
    public string NumeroCredito { get; private set; }
    public string NumeroNfse { get; private set; }
    public DateTime DataConstituicao { get; private set; }
    public decimal ValorIssqn { get; private set; }
    public string TipoCredito { get; private set; }
    public bool SimplesNacional { get; private set; }
    public decimal Aliquota { get; private set; }
    public decimal ValorFaturado { get; private set; }
    public decimal ValorDeducao { get; private set; }
    public decimal BaseCalculo { get; private set; }

    private Credito() { }

    public Credito(
        string numeroCredito,
        string numeroNfse,
        DateTime dataConstituicao,
        decimal valorIssqn,
        string tipoCredito,
        bool simplesNacional,
        decimal aliquota,
        decimal valorFaturado,
        decimal valorDeducao,
        decimal baseCalculo)
    {
        NumeroCredito = numeroCredito;
        NumeroNfse = numeroNfse;
        DataConstituicao = dataConstituicao;
        ValorIssqn = valorIssqn;
        TipoCredito = tipoCredito;
        SimplesNacional = simplesNacional;
        Aliquota = aliquota;
        ValorFaturado = valorFaturado;
        ValorDeducao = valorDeducao;
        BaseCalculo = baseCalculo;
    }
}