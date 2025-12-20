namespace CreditosApi.Infrastructure.Messaging.Interfaces;

public interface IServiceBusConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
