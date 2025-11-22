using Microsoft.EntityFrameworkCore;
using CurrencyExchange.DAL.Data;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Repositories;
using CurrencyExchange.BLL.Interfaces;
using CurrencyExchange.BLL.Services;

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

// Adapters
builder.Services.AddScoped<IExchangeRateAdapter, CurrencyExchange.BLL.Adapters.PrivatBankAdapter>();
builder.Services.AddScoped<IExchangeRateAdapter, CurrencyExchange.BLL.Adapters.NbuAdapter>();

// Background Service для автооновлення курсів
builder.Services.AddHostedService<ExchangeRateBackgroundService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Seed данихъ
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await CurrencyExchange.DAL.DbInitializer.SeedDataAsync(context);
}

app.Run();