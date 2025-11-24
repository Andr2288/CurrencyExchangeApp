using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.Services
{
    public class DynamicExchangeRateBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DynamicExchangeRateBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _updateIntervalMinutes;

        public DynamicExchangeRateBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DynamicExchangeRateBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _updateIntervalMinutes = _configuration.GetValue<int>("ExchangeRateSettings:UpdateIntervalMinutes", 10);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Dynamic Exchange Rate Background Service started. Update interval: {Minutes} minutes", _updateIntervalMinutes);

            // Перше оновлення одразу при запуску
            await FetchRatesAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(_updateIntervalMinutes), stoppingToken);
                    await FetchRatesAsync();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Dynamic background service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in dynamic background service");
                }
            }
        }

        private async Task FetchRatesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var fetchService = scope.ServiceProvider.GetRequiredService<DynamicExchangeRateFetchService>();

            try
            {
                var startTime = DateTime.Now;
                _logger.LogInformation("Dynamic background fetch started at {Time}", startTime.ToString("HH:mm:ss"));

                var count = await fetchService.FetchAllRatesAsync();

                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalSeconds;
                _logger.LogInformation("Dynamic background fetch completed at {Time}. Fetched {Count} rates. Duration: {Duration:F1}s",
                    endTime.ToString("HH:mm:ss"), count, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching rates in dynamic background service at {Time}", DateTime.Now.ToString("HH:mm:ss"));
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Dynamic Exchange Rate Background Service is stopping");
            return base.StopAsync(cancellationToken);
        }
    }
}
