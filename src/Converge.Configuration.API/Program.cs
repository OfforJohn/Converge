using Converge.Configuration.Services;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using System.Text.Json.Serialization;
using Converge.Configuration.API.Json;

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

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
