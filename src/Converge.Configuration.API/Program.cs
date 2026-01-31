using System;
using System.Linq;
using Confluent.Kafka;
using Converge.Configuration.Application.Events;
using Converge.Configuration.Application.Handlers;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.Application.Handlers.Implementations;
using Converge.Configuration.Services;
using Converge.Configuration.API.Json;
using Converge.Configuration.API.Middleware;
using Converge.Configuration.API.Services;
using Converge.Configuration.Application.Services;
using Converge.Configuration.Persistence;
using Microsoft.EntityFrameworkCore;
using ConvergeERP.Shared.Abstractions;
using Converge.Configuration.API.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.



// Register IHttpContextAccessor and ICurrentUser for DI
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Configure controllers and JSON options to allow enum values as strings
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // use case-insensitive enum converter so client may send "Global" or "global" or camelCase
        opts.JsonSerializerOptions.Converters.Add(new CaseInsensitiveEnumConverterFactory());
    });



// Configure Postgres DbContext for audit/event persistence if Persistence:UsePostgres=true
var usePostgres = builder.Configuration.GetValue<bool>("Persistence:UsePostgres", false);
// New flag: control whether to apply EF migrations automatically on startup
var applyMigrationsOnStartup = builder.Configuration.GetValue<bool>("Persistence:ApplyMigrationsOnStartup", true);

// Debug: print environment and any configuration keys containing "Postgres" so we can see what's actually loaded
Console.WriteLine("ENVIRONMENT: " + builder.Environment.EnvironmentName);
foreach (var kv in builder.Configuration.AsEnumerable()
             .Where(kv => kv.Key.IndexOf("Postgres", StringComparison.OrdinalIgnoreCase) >= 0))
{
    Console.WriteLine($"{kv.Key} = {kv.Value}");
}

if (usePostgres)
{
    var connection =
        builder.Configuration.GetConnectionString("Postgres")
        ?? builder.Configuration["Persistence:PostgresConnection"]
        ?? throw new InvalidOperationException("Postgres connection string not configured");
    
    // Register DbContext FIRST
    builder.Services.AddDbContext<ConfigurationDbContext>(opt =>
    {
        // Configure Npgsql provider with the connection string
        opt.UseNpgsql(connection, b => b.MigrationsAssembly("Converge.Configuration.Persistence"));

        // Enable sensitive data logging and detailed errors in development so EF Core will
        // include parameter values and richer error messages in the logs. This helps when
        // debugging SQL and seeing actual input values (e.g. payloads) that are sent to the DB.
        // IMPORTANT: Do NOT enable this in production — it can expose PII and secrets in logs.
        if (builder.Environment.IsDevelopment())
        {
            // Shows parameter values in EF Core logs (DEV ONLY)
            opt.EnableSensitiveDataLogging();
            // Enable more detailed EF exceptions (DEV ONLY)
            opt.EnableDetailedErrors();
        }
    });

    // Register services that depend on DbContext
    builder.Services.AddScoped<IAuditService, Converge.Configuration.Application.Services.ConsoleAuditService>();
    builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
    builder.Services.AddScoped<IConfigService, DbConfigService>();

    // Register context and scope services
    builder.Services.AddScoped<IScopeContext, HttpScopeContext>();
    builder.Services.AddScoped<ITokenScopeService, TokenScopeService>();
}
else
{
    // Fallback to console implementations for dev
    builder.Services.AddSingleton<IAuditService, Converge.Configuration.Application.Services.ConsoleAuditService>();
    builder.Services.AddSingleton<IEventPublisher, Converge.Configuration.Application.Events.OutboxEventPublisher>();
    builder.Services.AddSingleton<IConfigService, InMemoryConfigService>();

    // Register context and scope services
    builder.Services.AddScoped<IScopeContext, HttpScopeContext>();
    builder.Services.AddScoped<ITokenScopeService, TokenScopeService>();
}

// Register request dispatcher and handlers
builder.Services.AddScoped<IRequestDispatcher, RequestDispatcher>();

// register all handlers
builder.Services.AddTransient<IRequestHandler<GetConfigQuery, Converge.Configuration.DTOs.ConfigResponse?>, GetConfigHandler>();
builder.Services.AddTransient<IRequestHandler<CreateConfigCommand, Converge.Configuration.DTOs.ConfigResponse>, CreateConfigHandler>();
builder.Services.AddTransient<IRequestHandler<UpdateConfigCommand, Converge.Configuration.DTOs.ConfigResponse?>, UpdateConfigHandler>();
builder.Services.AddTransient<IRequestHandler<RollbackConfigCommand, Converge.Configuration.DTOs.ConfigResponse?>, RollbackConfigHandler>();

// Caching feature toggle (set Caching:Enabled=true in appsettings or environment to enable Redis caching)
var cachingEnabled = builder.Configuration.GetValue<bool>("Caching:Enabled", false);


if (cachingEnabled)
{
    // Configure Redis distributed cache (reads connection string from appsettings: "Redis:Configuration" or ConnectionStrings:Redis)
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? builder.Configuration["Redis:Configuration"] ?? "localhost:6379";
    });

    // Decorate with cached service (resolve IDistributedCache and inner service)
    builder.Services.Decorate<IConfigService, CachedConfigService>();
}
else
{
    // No caching: leave the plain in-memory service as the IConfigService implementation.
}

// Simple test authorization policies so Postman requests with no auth succeed in development.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanReadConfig", policy => policy.RequireAssertion(_ => true));
    options.AddPolicy("CanWriteConfig", policy => policy.RequireAssertion(_ => true));
});

Console.WriteLine("Effective Postgres: " + builder.Configuration.GetConnectionString("Postgres"));

// Register the custom scope filter
builder.Services.AddScoped<IScopeFilter, MyScopeFilter>();

// Apply pending EF Core migrations before registering Kafka to avoid background host services
// attempting to use the database or Kafka before migrations are applied.
if (usePostgres)
{
    if (applyMigrationsOnStartup)
    {
        try
        {
            using var tempProvider = builder.Services.BuildServiceProvider();
            using var scope = tempProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            Console.WriteLine("Applying pending EF Core migrations...");
            db.Database.Migrate();
            Console.WriteLine("Database migrations applied.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to apply migrations: {ex}");
        }
    }
    else
    {
        Console.WriteLine("Skipping applying EF Core migrations on startup (Persistence:ApplyMigrationsOnStartup=false).");
    }

    // Now it's safe to register KafkaDispatcher; if migrations failed above we'll still avoid
    // registering Kafka if no bootstrap servers are configured.
    var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"];
    if (!string.IsNullOrEmpty(kafkaBootstrap))
    {
        var producerConfig = new ProducerConfig { BootstrapServers = kafkaBootstrap };
        builder.Services.AddSingleton(producerConfig);
        builder.Services.AddHostedService<KafkaDispatcher>(sp => new KafkaDispatcher(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<KafkaDispatcher>>(),
            producerConfig,
            sp.GetRequiredService<IConfiguration>()));
    }
}
// builder.Services.AddApplicationInsightsTelemetry(); // Package not installed

var app = builder.Build();



// Configure the HTTP request pipeline.
// Add exception handling middleware early to translate domain exceptions into HTTP responses.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();



app.UseAuthorization();

app.MapControllers();

app.Run();