using Microsoft.EntityFrameworkCore;
using CurrencyExchange.DAL.Data;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Repositories;
using CurrencyExchange.BLL.Interfaces;
using CurrencyExchange.BLL.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
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
builder.Services.AddScoped<ExchangeRateFetchService>();
builder.Services.AddScoped<CurrencyConversionService>();
builder.Services.AddScoped<AuthService>();

// Adapters
builder.Services.AddScoped<IExchangeRateAdapter, CurrencyExchange.BLL.Adapters.PrivatBankAdapter>();
builder.Services.AddScoped<IExchangeRateAdapter, CurrencyExchange.BLL.Adapters.NbuAdapter>();

// Background Service для автооновлення курсів
builder.Services.AddHostedService<ExchangeRateBackgroundService>();

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

// CORS - ВАЖЛИВО: додаємо обидва порти
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",    // Vite dev server
                "http://localhost:5099",    // Якщо запускається на цьому порті
                "https://localhost:7287"    // HTTPS порт
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowed(origin => true); // Для development
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Вимикаємо HTTPS redirect в development для спрощення
    // app.UseHttpsRedirection();
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

// Seed данихъ
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await CurrencyExchange.DAL.DbInitializer.SeedDataAsync(context);
}

app.Run();