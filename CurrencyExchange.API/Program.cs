using CurrencyExchange.BLL.Interfaces;
using CurrencyExchange.BLL.Services;
using CurrencyExchange.DAL.Data;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using CurrencyExchange.DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();

// HttpClient
builder.Services.AddHttpClient();

// Services
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
builder.Services.AddScoped<CurrencyConversionService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LogService>();

// NEW: Dynamic Exchange Rate System
// Реєструємо динамічний сервіс замість статичного
builder.Services.AddScoped<DynamicExchangeRateFetchService>();

builder.Services.AddScoped<CurrencyExchange.BLL.Adapters.PrivatBankAdapter>(provider =>
    new CurrencyExchange.BLL.Adapters.PrivatBankAdapter(
        provider.GetRequiredService<HttpClient>(),
        provider.GetRequiredService<IRepository<Currency>>(),
        provider.GetRequiredService<IRepository<ApiSource>>(),
        provider.GetRequiredService<IRepository<ExchangeRate>>(), // ДОДАНО
        provider.GetRequiredService<ILogger<CurrencyExchange.BLL.Adapters.PrivatBankAdapter>>()
    ));

builder.Services.AddScoped<CurrencyExchange.BLL.Adapters.NbuAdapter>(provider =>
    new CurrencyExchange.BLL.Adapters.NbuAdapter(
        provider.GetRequiredService<HttpClient>(),
        provider.GetRequiredService<IRepository<Currency>>(),
        provider.GetRequiredService<IRepository<ApiSource>>(),
        provider.GetRequiredService<IRepository<ExchangeRate>>(), // ДОДАНО
        provider.GetRequiredService<ILogger<CurrencyExchange.BLL.Adapters.NbuAdapter>>()
    ));

// OLD: Static adapters (kept for backwards compatibility)
builder.Services.AddScoped<ExchangeRateFetchService>();
builder.Services.AddScoped<IExchangeRateAdapter, CurrencyExchange.BLL.Adapters.PrivatBankAdapter>();
builder.Services.AddScoped<IExchangeRateAdapter, CurrencyExchange.BLL.Adapters.NbuAdapter>();

// Background Service для автооновлення курсів з новою системою
builder.Services.AddHostedService<DynamicExchangeRateBackgroundService>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ??
                    "super-secret-key-minimum-32-characters-long-for-security"))
        };
    });

// CSRF Protection - Antiforgery tokens
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = false; // Щоб frontend міг прочитати
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Для dev (в prod змінити на Always)
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5099",
                "https://localhost:7287"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowed(origin => true);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

// CORS має бути ПЕРЕД іншими middleware
app.UseCors("AllowFrontend");

// Rate Limiting middleware
app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed даних
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await CurrencyExchange.DAL.DbInitializer.SeedDataAsync(context);
}

app.Run();