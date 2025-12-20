using CreditosApi.Infrastructure.Messaging.Interfaces;

namespace CreditosApi.Worker.Services
{
    public class ServiceBusWorker : BackgroundService
    {
        private readonly IServiceBusConsumer _serviceBusConsumer;
        private readonly ILogger<ServiceBusWorker> _logger;

        public ServiceBusWorker(
            IServiceBusConsumer serviceBusConsumer,
            ILogger<ServiceBusWorker> logger)
        {
            _serviceBusConsumer = serviceBusConsumer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service Bus Worker iniciado");

            await _serviceBusConsumer.StartAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(500, stoppingToken); // Verifica a cada 500ms
            }

            await _serviceBusConsumer.StopAsync(stoppingToken);
            _logger.LogInformation("Service Bus Worker finalizado");
        }
    }
}