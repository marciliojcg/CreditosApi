using CreditosApi.Application.Interfaces.Messaging;
using CreditosApi.Domain.Interfaces;
using CreditosApi.Infrastructure.Data;
using CreditosApi.Infrastructure.Messaging;
using CreditosApi.Infrastructure.Messaging.Interfaces;
using CreditosApi.Infrastructure.Repositories;
using CreditosApi.Worker.Services;
using Microsoft.EntityFrameworkCore;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<ICreditoRepository, CreditoRepository>();

        // Processors
        services.AddScoped<ICreditoProcessor, CreditoProcessor>();

        // Service Bus
        services.AddSingleton<IServiceBusConsumer>(provider =>
        {
            var connectionString = context.Configuration["ServiceBus:ConnectionString"];
            var queueName = context.Configuration["ServiceBus:QueueName"];
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            var logger = provider.GetRequiredService<ILogger<ServiceBusConsumer>>();

            return new ServiceBusConsumer(connectionString, queueName, scopeFactory, logger);
        });

        // Worker Service
        services.AddHostedService<ServiceBusWorker>();
    })
    .Build();

await host.RunAsync();