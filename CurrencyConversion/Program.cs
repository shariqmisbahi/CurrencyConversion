using CurrencyConversion.Policies;
using CurrencyConversion.Providers;
using CurrencyConversion.Services;
using System;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
//dotnet add package Serilog
//dotnet add package Serilog.AspNetCore
//dotnet add package Serilog.Sinks.Console
//dotnet add package Serilog.Sinks.Seq  # If using Seq
//using Serilog.l

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add memory cache
builder.Services.AddMemoryCache();

// Add HTTP client with resilience policies
builder.Services.AddHttpClient<FrankfurterProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.frankfurter.app/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddPolicyHandler(ResiliencePolicies.GetRetryPolicy(builder.Services.BuildServiceProvider().GetRequiredService<ILogger<FrankfurterProvider>>()))
.AddPolicyHandler(ResiliencePolicies.GetCircuitBreakerPolicy(builder.Services.BuildServiceProvider().GetRequiredService<ILogger<FrankfurterProvider>>()));
// Generate a secure random key (run once and store the result)
var key = Convert.ToBase64String(new byte[32]); // 256-bit key
Console.WriteLine(key);

//// Add JWT Authentication
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = builder.Configuration["Jwt:Issuer"],
//            ValidAudience = builder.Configuration["Jwt:Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(
//                Encoding.UTF8.GetBytes(key))
//        };
//    });

//// Add Authorization with RBAC
//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("RequireAdminRole", policy =>
//        policy.RequireRole("Admin"));
//    options.AddPolicy("RequireUserRole", policy =>
//        policy.RequireRole("User", "Admin"));
//});

builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
}).UseSerilog((hostingContext, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(hostingContext.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq("http://localhost:5341");
});


//// Add OpenTelemetry
//builder.Services.AddOpenTelemetry()
//    .WithTracing(tracing =>
//    {
//        tracing.AddAspNetCoreInstrumentation()
//            .AddHttpClientInstrumentation()
//            .AddOtlpExporter();
//    });

// Register services
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSingleton<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();
builder.Services.AddScoped<FrankfurterProvider>();

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

app.Run();