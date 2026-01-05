using Converge.Configuration.Application.Handlers;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.Application.Handlers.Implementations;
using Converge.Configuration.Services;
using Converge.Configuration.API.Json;
using Converge.Configuration.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure controllers and JSON options to allow enum values as strings
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // use case-insensitive enum converter so client may send "Global" or "global" or camelCase
        opts.JsonSerializerOptions.Converters.Add(new CaseInsensitiveEnumConverterFactory());
    });

// Register in-memory config service for the API and tests
builder.Services.AddSingleton<IConfigService, InMemoryConfigService>();

// Register request dispatcher and handlers
builder.Services.AddSingleton<IRequestDispatcher, RequestDispatcher>();

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

// Simple test authorization policies so Postman requests with no auth succeed in development.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanReadConfig", policy => policy.RequireAssertion(_ => true));
    options.AddPolicy("CanWriteConfig", policy => policy.RequireAssertion(_ => true));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Add exception handling middleware early to translate domain exceptions into HTTP responses.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();