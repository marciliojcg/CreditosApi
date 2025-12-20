namespace CreditosApi.Application.Interfaces.Messaging;

public interface ICreditoProcessor
{
    Task ProcessarCreditoAsync(Domain.Events.CreditoRecebidoEvent creditoEvent);
}
