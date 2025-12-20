using CreditosApi.Application.Interfaces;
using CreditosApi.Application.Interfaces.Messaging;
using CreditosApi.Application.Services;
using CreditosApi.Domain.Interfaces;
using CreditosApi.Infrastructure.Data;
using CreditosApi.Infrastructure.Messaging;
using CreditosApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories (Infrastructure implementations of Domain interfaces)
builder.Services.AddScoped<ICreditoRepository, CreditoRepository>();

// Messaging (Infrastructure implementations of Application interfaces)
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

// Application Services
builder.Services.AddScoped<ICreditoService, CreditoService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Database Migration
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();