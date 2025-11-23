using CurrencyExchange.BLL.Interfaces;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.Services
{
    public class ExchangeRateFetchService
    {
        private readonly IEnumerable<IExchangeRateAdapter> _adapters;
        private readonly IRepository<ApiSource> _apiSourceRepository;
        private readonly ILogger<ExchangeRateFetchService> _logger;
        private readonly LogService _logService;

        public ExchangeRateFetchService(
            IEnumerable<IExchangeRateAdapter> adapters,
            IRepository<ApiSource> apiSourceRepository,
            ILogger<ExchangeRateFetchService> logger,
            LogService logService)
        {
            _adapters = adapters;
            _apiSourceRepository = apiSourceRepository;
            _logger = logger;
            _logService = logService;
        }

        public async Task<int> FetchAllRatesAsync()
        {
            int totalCount = 0;

            await _logService.LogAsync("Info", "ExchangeRateFetchService", "Початок оновлення всіх курсів");

            foreach (var adapter in _adapters)
            {
                try
                {
                    var rates = await adapter.FetchRatesAsync();
                    totalCount += rates.Count;

                    var sourceName = adapter.GetSourceName();
                    await _logService.LogAsync("Info", sourceName, $"Завантажено {rates.Count} курсів");

                    await UpdateSourceTimestamp(sourceName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching rates from {Source}", adapter.GetSourceName());
                    await _logService.LogAsync("Error", adapter.GetSourceName(), $"Помилка: {ex.Message}");
                }
            }

            await _logService.LogAsync("Info", "ExchangeRateFetchService", $"Завершено оновлення. Всього: {totalCount} курсів");
            return totalCount;
        }

        public async Task<int> FetchBySourceAsync(string sourceName)
        {
            await _logService.LogAsync("Info", sourceName, "Початок оновлення курсів");

            var adapter = _adapters.FirstOrDefault(a => a.GetSourceName() == sourceName);

            if (adapter == null)
            {
                await _logService.LogAsync("Warning", sourceName, "Адаптер не знайдено");
                return 0;
            }

            try
            {
                var rates = await adapter.FetchRatesAsync();
                await UpdateSourceTimestamp(sourceName);

                await _logService.LogAsync("Info", sourceName, $"Завантажено {rates.Count} курсів");
                return rates.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching rates from {Source}", sourceName);
                await _logService.LogAsync("Error", sourceName, $"Помилка: {ex.Message}");
                return 0;
            }
        }

        private async Task UpdateSourceTimestamp(string sourceName)
        {
            var sources = await _apiSourceRepository.FindAsync(s => s.Name == sourceName);
            var source = sources.FirstOrDefault();

            if (source != null)
            {
                source.LastUpdateAt = DateTime.UtcNow;
                await _apiSourceRepository.UpdateAsync(source);
                await _apiSourceRepository.SaveChangesAsync();
            }
        }
    }
}
