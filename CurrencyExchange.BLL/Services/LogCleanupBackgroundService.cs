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
    public class LogCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LogCleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Щодня

        public LogCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<LogCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Log Cleanup Background Service started. Cleanup interval: 24 hours");

            // Перше очищення через 1 хвилину після запуску
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            await CleanupLogsAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                    await CleanupLogsAsync();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Log cleanup service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in log cleanup service");
                }
            }
        }

        private async Task CleanupLogsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<LogService>();

            try
            {
                _logger.LogInformation("Starting log cleanup at {Time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                var deletedCount = await logService.CleanupOldLogsAsync();

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Log cleanup completed. Deleted {Count} old log entries", deletedCount);
                }
                else
                {
                    _logger.LogInformation("Log cleanup completed. No old logs to delete");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during log cleanup");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Log Cleanup Background Service is stopping");
            return base.StopAsync(cancellationToken);
        }
    }
}
