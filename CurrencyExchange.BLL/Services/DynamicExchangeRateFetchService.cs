using CurrencyExchange.BLL.Interfaces;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.Services
{
    /// <summary>
    /// Динамічний сервіс для створення адаптерів на основі налаштувань в БД
    /// </summary>
    public class DynamicExchangeRateFetchService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRepository<ApiSource> _apiSourceRepository;
        private readonly ILogger<DynamicExchangeRateFetchService> _logger;
        private readonly LogService _logService;

        public DynamicExchangeRateFetchService(
            IServiceProvider serviceProvider,
            IRepository<ApiSource> apiSourceRepository,
            ILogger<DynamicExchangeRateFetchService> logger,
            LogService logService)
        {
            _serviceProvider = serviceProvider;
            _apiSourceRepository = apiSourceRepository;
            _logger = logger;
            _logService = logService;
        }

        public async Task<int> FetchAllRatesAsync()
        {
            int totalCount = 0;

            await _logService.LogAsync("Info", "DynamicExchangeRateFetchService", "Початок оновлення всіх курсів");

            // Отримуємо всі активні джерела з БД
            var activeSources = await _apiSourceRepository.FindAsync(s => s.IsActive);

            _logger.LogInformation("Found {Count} active API sources", activeSources.Count());

            foreach (var apiSource in activeSources)
            {
                try
                {
                    var adapter = CreateAdapterForSource(apiSource);
                    var rates = await adapter.FetchRatesAsync();
                    totalCount += rates.Count;

                    await _logService.LogAsync("Info", apiSource.Name, $"Завантажено {rates.Count} курсів");
                    await UpdateSourceTimestamp(apiSource);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching rates from {Source}", apiSource.Name);
                    await _logService.LogAsync("Error", apiSource.Name, $"Помилка: {ex.Message}");
                }
            }

            await _logService.LogAsync("Info", "DynamicExchangeRateFetchService", $"Завершено оновлення. Всього: {totalCount} курсів");
            return totalCount;
        }

        public async Task<int> FetchBySourceAsync(string sourceName)
        {
            await _logService.LogAsync("Info", sourceName, "Початок оновлення курсів");

            var sources = await _apiSourceRepository.FindAsync(s => s.Name == sourceName && s.IsActive);
            var apiSource = sources.FirstOrDefault();

            if (apiSource == null)
            {
                await _logService.LogAsync("Warning", sourceName, "Джерело не знайдено або неактивне");
                return 0;
            }

            try
            {
                var adapter = CreateAdapterForSource(apiSource);
                var rates = await adapter.FetchRatesAsync();

                await UpdateSourceTimestamp(apiSource);
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

        /// <summary>
        /// Створює адаптер для конкретного джерела API
        /// </summary>
        private IExchangeRateAdapter CreateAdapterForSource(ApiSource apiSource)
        {
            // Перевіряємо, чи це legacy адаптер (ПриватБанк, НБУ)
            switch (apiSource.Name)
            {
                case "ПриватБанк":
                    return CreateLegacyAdapter<CurrencyExchange.BLL.Adapters.PrivatBankAdapter>();

                case "НБУ":
                    return CreateLegacyAdapter<CurrencyExchange.BLL.Adapters.NbuAdapter>();

                default:
                    // Для всіх нових джерел створюємо універсальний адаптер
                    return CreateUniversalAdapter(apiSource);
            }
        }

        /// <summary>
        /// Створює legacy адаптер (ПриватБанк, НБУ)
        /// </summary>
        private T CreateLegacyAdapter<T>() where T : IExchangeRateAdapter
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Створює універсальний адаптер для нових API джерел
        /// </summary>
        private IExchangeRateAdapter CreateUniversalAdapter(ApiSource apiSource)
        {
            using var scope = _serviceProvider.CreateScope();

            var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var currencyRepository = scope.ServiceProvider.GetRequiredService<IRepository<Currency>>();
            var apiSourceRepository = scope.ServiceProvider.GetRequiredService<IRepository<ApiSource>>();
            var exchangeRateRepository = scope.ServiceProvider.GetRequiredService<IRepository<ExchangeRate>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CurrencyExchange.BLL.Adapters.UniversalApiAdapter>>();

            return new CurrencyExchange.BLL.Adapters.UniversalApiAdapter(
                httpClient,
                currencyRepository,
                apiSourceRepository,
                exchangeRateRepository,
                logger,
                apiSource
            );
        }

        private async Task UpdateSourceTimestamp(ApiSource apiSource)
        {
            try
            {
                apiSource.LastUpdateAt = DateTime.UtcNow;
                await _apiSourceRepository.UpdateAsync(apiSource);
                await _apiSourceRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timestamp for source {Source}", apiSource.Name);
            }
        }

        /// <summary>
        /// Отримати список всіх доступних адаптерів
        /// </summary>
        public async Task<List<string>> GetAvailableSourcesAsync()
        {
            var activeSources = await _apiSourceRepository.FindAsync(s => s.IsActive);
            return activeSources.Select(s => s.Name).ToList();
        }
    }
}
