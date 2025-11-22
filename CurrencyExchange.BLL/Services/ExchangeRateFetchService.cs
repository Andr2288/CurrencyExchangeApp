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
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly IRepository<ApiSource> _apiSourceRepository;
        private readonly ILogger<ExchangeRateFetchService> _logger;

        public ExchangeRateFetchService(
            IEnumerable<IExchangeRateAdapter> adapters,
            IExchangeRateRepository exchangeRateRepository,
            IRepository<ApiSource> apiSourceRepository,
            ILogger<ExchangeRateFetchService> logger)
        {
            _adapters = adapters;
            _exchangeRateRepository = exchangeRateRepository;
            _apiSourceRepository = apiSourceRepository;
            _logger = logger;
        }

        public async Task<int> FetchAllRatesAsync()
        {
            _logger.LogInformation("Starting to fetch rates from all sources");

            int totalFetched = 0;

            foreach (var adapter in _adapters)
            {
                try
                {
                    var rates = await adapter.FetchRatesAsync();

                    if (rates.Any())
                    {
                        // Зберігаємо курси в БД
                        foreach (var rate in rates)
                        {
                            await _exchangeRateRepository.AddAsync(rate);
                        }
                        await _exchangeRateRepository.SaveChangesAsync();

                        // Оновлюємо час останнього оновлення джерела
                        var apiSources = await _apiSourceRepository.FindAsync(s => s.Name == adapter.GetSourceName());
                        var apiSource = apiSources.FirstOrDefault();
                        if (apiSource != null)
                        {
                            apiSource.LastUpdateAt = DateTime.UtcNow;
                            await _apiSourceRepository.UpdateAsync(apiSource);
                            await _apiSourceRepository.SaveChangesAsync();
                        }

                        totalFetched += rates.Count;
                        _logger.LogInformation($"Saved {rates.Count} rates from {adapter.GetSourceName()}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error fetching rates from {adapter.GetSourceName()}");
                }
            }

            _logger.LogInformation($"Finished fetching rates. Total: {totalFetched}");
            return totalFetched;
        }

        public async Task<int> FetchRatesBySourceAsync(string sourceName)
        {
            _logger.LogInformation($"Fetching rates from {sourceName}");

            var adapter = _adapters.FirstOrDefault(a => a.GetSourceName() == sourceName);
            if (adapter == null)
            {
                _logger.LogWarning($"Adapter for {sourceName} not found");
                return 0;
            }

            try
            {
                var rates = await adapter.FetchRatesAsync();

                if (rates.Any())
                {
                    foreach (var rate in rates)
                    {
                        await _exchangeRateRepository.AddAsync(rate);
                    }
                    await _exchangeRateRepository.SaveChangesAsync();

                    // Оновлюємо час останнього оновлення
                    var apiSources = await _apiSourceRepository.FindAsync(s => s.Name == sourceName);
                    var apiSource = apiSources.FirstOrDefault();
                    if (apiSource != null)
                    {
                        apiSource.LastUpdateAt = DateTime.UtcNow;
                        await _apiSourceRepository.UpdateAsync(apiSource);
                        await _apiSourceRepository.SaveChangesAsync();
                    }

                    _logger.LogInformation($"Saved {rates.Count} rates from {sourceName}");
                    return rates.Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching rates from {sourceName}");
            }

            return 0;
        }
    }
}
