using CurrencyExchange.BLL.DTOs;
using CurrencyExchange.BLL.Interfaces;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.Adapters
{
    public class PrivatBankAdapter : IExchangeRateAdapter
    {
        private readonly HttpClient _httpClient;
        private readonly IRepository<Currency> _currencyRepository;
        private readonly IRepository<ApiSource> _apiSourceRepository;
        private readonly ILogger<PrivatBankAdapter> _logger;
        private const string API_URL = "https://api.privatbank.ua/p24api/pubinfo?json&exchange&coursid=5";

        public PrivatBankAdapter(
            HttpClient httpClient,
            IRepository<Currency> currencyRepository,
            IRepository<ApiSource> apiSourceRepository,
            ILogger<PrivatBankAdapter> logger)
        {
            _httpClient = httpClient;
            _currencyRepository = currencyRepository;
            _apiSourceRepository = apiSourceRepository;
            _logger = logger;
        }

        public string GetSourceName() => "ПриватБанк";

        public async Task<List<ExchangeRate>> FetchRatesAsync()
        {
            var rates = new List<ExchangeRate>();

            try
            {
                _logger.LogInformation("Fetching rates from PrivatBank API");

                var response = await _httpClient.GetStringAsync(API_URL);
                _logger.LogDebug("PrivatBank API response: {Response}", response);

                var privatRates = JsonSerializer.Deserialize<List<PrivatBankRateDto>>(response);

                if (privatRates == null || !privatRates.Any())
                {
                    _logger.LogWarning("No rates returned from PrivatBank API");
                    return rates;
                }

                // Отримуємо всі валюти та джерело
                var currencies = await _currencyRepository.GetAllAsync();
                var apiSources = await _apiSourceRepository.FindAsync(s => s.Name == GetSourceName());
                var apiSource = apiSources.FirstOrDefault();

                if (apiSource == null)
                {
                    _logger.LogError("ApiSource 'ПриватБанк' not found in database");
                    return rates;
                }

                var uah = currencies.FirstOrDefault(c => c.Code == "UAH");
                if (uah == null)
                {
                    _logger.LogError("UAH currency not found in database");
                    return rates;
                }

                foreach (var rate in privatRates)
                {
                    var currency = currencies.FirstOrDefault(c => c.Code == rate.ccy);
                    if (currency == null)
                    {
                        _logger.LogDebug($"Currency {rate.ccy} not found in database, skipping");
                        continue;
                    }

                    // Покращена валідація курсів з підтримкою різних форматів
                    if (!TryParseDecimal(rate.buy, out var buyRate) ||
                        !TryParseDecimal(rate.sale, out var sellRate))
                    {
                        _logger.LogWarning($"Invalid rate format for {rate.ccy}: buy={rate.buy}, sale={rate.sale}");
                        continue;
                    }

                    if (buyRate <= 0 || sellRate <= 0)
                    {
                        _logger.LogWarning($"Invalid rate values for {rate.ccy}: buy={buyRate}, sell={sellRate}");
                        continue;
                    }

                    rates.Add(new ExchangeRate
                    {
                        FromCurrencyId = currency.Id,
                        ToCurrencyId = uah.Id,
                        ApiSourceId = apiSource.Id,
                        BuyRate = buyRate,
                        SellRate = sellRate,
                        FetchedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });

                    _logger.LogDebug($"Successfully parsed rate for {rate.ccy}: buy={buyRate}, sell={sellRate}");
                }

                _logger.LogInformation($"Successfully fetched {rates.Count} rates from PrivatBank");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching rates from PrivatBank");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error for PrivatBank response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching rates from PrivatBank");
            }

            return rates;
        }

        private bool TryParseDecimal(string value, out decimal result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Спробуємо різні варіанти парсингу
            var formats = new[]
            {
                CultureInfo.InvariantCulture,
                CultureInfo.GetCultureInfo("uk-UA"),
                CultureInfo.GetCultureInfo("en-US")
            };

            foreach (var culture in formats)
            {
                if (decimal.TryParse(value, NumberStyles.Number, culture, out result))
                {
                    return true;
                }
            }

            // Спробуємо замінити кому на крапку та навпаки
            var normalizedValue = value.Replace(',', '.');
            if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }

            normalizedValue = value.Replace('.', ',');
            if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.GetCultureInfo("uk-UA"), out result))
            {
                return true;
            }

            return false;
        }
    }
}
