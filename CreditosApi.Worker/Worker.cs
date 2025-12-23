namespace CreditosApi.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public Worker(
        ILogger<Worker> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Worker iniciado em: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Worker executando em: {time}", DateTimeOffset.Now);
                    }

                    await Task.Delay(5000, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker recebeu solicitação de parada");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante execução do worker");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Erro crítico no worker");
            _hostApplicationLifetime.StopApplication();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker finalizando em: {time}", DateTimeOffset.Now);
        await base.StopAsync(cancellationToken);
    }
}
