using Azure.Messaging.ServiceBus;
using CreditosApi.Application.Interfaces.Messaging;
using CreditosApi.Infrastructure.Messaging.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CreditosApi.Infrastructure.Messaging;


public class ServiceBusConsumer : IServiceBusConsumer
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServiceBusConsumer> _logger;

    public ServiceBusConsumer(
        string connectionString,
        string queueName,
        IServiceScopeFactory scopeFactory,
        ILogger<ServiceBusConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _client = new ServiceBusClient(connectionString);
        _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions());

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var body = args.Message.Body.ToString();
            var creditoEvent = JsonSerializer.Deserialize<Domain.Events.CreditoRecebidoEvent>(body);

            if (creditoEvent != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var creditoProcessor = scope.ServiceProvider
                    .GetRequiredService<ICreditoProcessor>();

                await creditoProcessor.ProcessarCreditoAsync(creditoEvent);
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem do Service Bus");
            await args.DeadLetterMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Erro no processamento do Service Bus");
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _processor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
    }
}