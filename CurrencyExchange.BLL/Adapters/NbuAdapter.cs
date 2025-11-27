using CurrencyExchange.BLL.DTOs;
using CurrencyExchange.BLL.Interfaces;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.Adapters
{
    public class NbuAdapter : IExchangeRateAdapter
    {
        private readonly HttpClient _httpClient;
        private readonly IRepository<Currency> _currencyRepository;
        private readonly IRepository<ApiSource> _apiSourceRepository;
        private readonly IRepository<ExchangeRate> _exchangeRateRepository; // ДОДАНО
        private readonly ILogger<NbuAdapter> _logger;
        private const string API_URL = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json";

        public NbuAdapter(
            HttpClient httpClient,
            IRepository<Currency> currencyRepository,
            IRepository<ApiSource> apiSourceRepository,
            IRepository<ExchangeRate> exchangeRateRepository, // ДОДАНО
            ILogger<NbuAdapter> logger)
        {
            _httpClient = httpClient;
            _currencyRepository = currencyRepository;
            _apiSourceRepository = apiSourceRepository;
            _exchangeRateRepository = exchangeRateRepository; // ДОДАНО
            _logger = logger;
        }

        public string GetSourceName() => "НБУ";

        public async Task<List<ExchangeRate>> FetchRatesAsync()
        {
            var rates = new List<ExchangeRate>();

            try
            {
                _logger.LogInformation("Fetching rates from NBU API");

                var response = await _httpClient.GetStringAsync(API_URL);
                var nbuRates = JsonSerializer.Deserialize<List<NbuRateDto>>(response);

                if (nbuRates == null || !nbuRates.Any())
                {
                    _logger.LogWarning("No rates returned from NBU API");
                    return rates;
                }

                // Отримуємо всі валюти та джерело
                var currencies = await _currencyRepository.GetAllAsync();
                var apiSources = await _apiSourceRepository.FindAsync(s => s.Name == GetSourceName());
                var apiSource = apiSources.FirstOrDefault();

                if (apiSource == null)
                {
                    _logger.LogError("ApiSource 'НБУ' not found in database");
                    return rates;
                }

                var uah = currencies.FirstOrDefault(c => c.Code == "UAH");
                if (uah == null)
                {
                    _logger.LogError("UAH currency not found in database");
                    return rates;
                }

                foreach (var rate in nbuRates)
                {
                    var currency = currencies.FirstOrDefault(c => c.Code == rate.cc);
                    if (currency == null)
                    {
                        continue; // Пропускаємо валюти, яких немає в нашій БД
                    }

                    // Валідація курсу
                    if (rate.rate <= 0)
                    {
                        _logger.LogWarning($"Invalid rate value for {rate.cc}: {rate.rate}");
                        continue;
                    }

                    // ВИПРАВЛЕНО: Створюємо та зберігаємо в БД
                    var exchangeRate = new ExchangeRate
                    {
                        FromCurrencyId = currency.Id,
                        ToCurrencyId = uah.Id,
                        ApiSourceId = apiSource.Id,
                        BuyRate = rate.rate,
                        SellRate = rate.rate, // НБУ дає тільки офіційний курс, тому buy = sell
                        FetchedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    // ЗБЕРІГАЄМО В БАЗУ ДАНИХ
                    await _exchangeRateRepository.AddAsync(exchangeRate);
                    await _exchangeRateRepository.SaveChangesAsync();

                    rates.Add(exchangeRate);
                }

                _logger.LogInformation($"Successfully fetched and saved {rates.Count} rates from NBU");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching rates from NBU");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error for NBU response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching rates from NBU");
            }

            return rates;
        }
    }
}
