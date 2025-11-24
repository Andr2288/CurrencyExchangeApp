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
    /// <summary>
    /// Універсальний адаптер для роботи з будь-якими API джерелами
    /// на основі конфігурації з бази даних
    /// </summary>
    public class UniversalApiAdapter : IExchangeRateAdapter
    {
        private readonly HttpClient _httpClient;
        private readonly IRepository<Currency> _currencyRepository;
        private readonly IRepository<ApiSource> _apiSourceRepository;
        private readonly IRepository<ExchangeRate> _exchangeRateRepository;
        private readonly ILogger<UniversalApiAdapter> _logger;
        private readonly ApiSource _apiSource;

        public UniversalApiAdapter(
            HttpClient httpClient,
            IRepository<Currency> currencyRepository,
            IRepository<ApiSource> apiSourceRepository,
            IRepository<ExchangeRate> exchangeRateRepository,
            ILogger<UniversalApiAdapter> logger,
            ApiSource apiSource)
        {
            _httpClient = httpClient;
            _currencyRepository = currencyRepository;
            _apiSourceRepository = apiSourceRepository;
            _exchangeRateRepository = exchangeRateRepository;
            _logger = logger;
            _apiSource = apiSource;
        }

        public string GetSourceName() => _apiSource.Name;

        public async Task<List<ExchangeRate>> FetchRatesAsync()
        {
            var rates = new List<ExchangeRate>();

            if (!_apiSource.IsActive)
            {
                _logger.LogInformation("API Source {Name} is inactive, skipping", _apiSource.Name);
                return rates;
            }

            try
            {
                _logger.LogInformation("Fetching rates from {Source} API: {Url}", _apiSource.Name, _apiSource.Url);

                var response = await _httpClient.GetStringAsync(_apiSource.Url);
                _logger.LogDebug("{Source} API response: {Response}", _apiSource.Name, response.Substring(0, Math.Min(200, response.Length)));

                // Намагаємось розпарсити JSON
                if (_apiSource.Format.ToUpper() == "JSON")
                {
                    rates = await ParseJsonResponse(response);
                }
                else
                {
                    _logger.LogWarning("Unsupported format: {Format} for source {Source}", _apiSource.Format, _apiSource.Name);
                }

                _logger.LogInformation("Successfully fetched {Count} rates from {Source}", rates.Count, _apiSource.Name);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching rates from {Source}", _apiSource.Name);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error for {Source} response", _apiSource.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching rates from {Source}", _apiSource.Name);
            }

            return rates;
        }

        private async Task<List<ExchangeRate>> ParseJsonResponse(string jsonResponse)
        {
            var rates = new List<ExchangeRate>();

            // Отримуємо валюти та UAH
            var currencies = await _currencyRepository.GetAllAsync();
            var uah = currencies.FirstOrDefault(c => c.Code == "UAH");

            if (uah == null)
            {
                _logger.LogError("UAH currency not found in database");
                return rates;
            }

            try
            {
                // Спочатку пробуємо розпарсити як масив об'єктів
                var jsonDocument = JsonDocument.Parse(jsonResponse);

                if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
                {
                    // Масив об'єктів - стандартний формат
                    foreach (var element in jsonDocument.RootElement.EnumerateArray())
                    {
                        var rate = await TryParseRateElement(element, currencies, uah);
                        if (rate != null)
                        {
                            rates.Add(rate);
                        }
                    }
                }
                else if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object)
                {
                    // Об'єкт - можливо, один курс або nested структура
                    var rate = await TryParseRateElement(jsonDocument.RootElement, currencies, uah);
                    if (rate != null)
                    {
                        rates.Add(rate);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON response from {Source}", _apiSource.Name);
            }

            return rates;
        }

        private async Task<ExchangeRate?> TryParseRateElement(JsonElement element, IEnumerable<Currency> currencies, Currency uah)
        {
            try
            {
                // Шукаємо поля з валютою
                string? currencyCode = null;
                decimal buyRate = 0;
                decimal sellRate = 0;

                // Можливі варіанти назв полів для валютного коду
                var currencyFields = new[] { "ccy", "cc", "currency", "currencyCode", "from", "code" };
                foreach (var field in currencyFields)
                {
                    if (element.TryGetProperty(field, out var prop))
                    {
                        currencyCode = prop.GetString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(currencyCode))
                {
                    return null;
                }

                // Знаходимо валюту в нашій БД
                var currency = currencies.FirstOrDefault(c => c.Code.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));
                if (currency == null)
                {
                    _logger.LogDebug("Currency {Code} not found in database, skipping", currencyCode);
                    return null;
                }

                // Шукаємо поля з курсами
                buyRate = TryGetDecimalValue(element, new[] { "buy", "buyRate", "bid", "rate", "value" });
                sellRate = TryGetDecimalValue(element, new[] { "sell", "sale", "sellRate", "ask", "rate", "value" });

                // Якщо є тільки один курс - використовуємо його для обох напрямків
                if (buyRate > 0 && sellRate == 0)
                {
                    sellRate = buyRate;
                }
                else if (sellRate > 0 && buyRate == 0)
                {
                    buyRate = sellRate;
                }

                // Валідація
                if (buyRate <= 0 || sellRate <= 0)
                {
                    _logger.LogWarning("Invalid rate values for {Currency}: buy={Buy}, sell={Sell}", currencyCode, buyRate, sellRate);
                    return null;
                }

                // Створюємо ExchangeRate і зберігаємо в БД
                var exchangeRate = new ExchangeRate
                {
                    FromCurrencyId = currency.Id,
                    ToCurrencyId = uah.Id,
                    ApiSourceId = _apiSource.Id,
                    BuyRate = buyRate,
                    SellRate = sellRate,
                    FetchedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                // Зберігаємо в базу даних
                await _exchangeRateRepository.AddAsync(exchangeRate);
                await _exchangeRateRepository.SaveChangesAsync();

                _logger.LogDebug("Successfully parsed rate for {Currency}: buy={Buy}, sell={Sell}", currencyCode, buyRate, sellRate);

                return exchangeRate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing rate element from {Source}", _apiSource.Name);
                return null;
            }
        }

        private decimal TryGetDecimalValue(JsonElement element, string[] possibleFields)
        {
            foreach (var field in possibleFields)
            {
                if (element.TryGetProperty(field, out var prop))
                {
                    // Спробуємо отримати як число
                    if (prop.ValueKind == JsonValueKind.Number)
                    {
                        if (prop.TryGetDecimal(out var decimalValue))
                        {
                            return decimalValue;
                        }
                    }
                    // Спробуємо отримати як рядок і розпарсити
                    else if (prop.ValueKind == JsonValueKind.String)
                    {
                        var stringValue = prop.GetString();
                        if (TryParseDecimal(stringValue, out var parsedValue))
                        {
                            return parsedValue;
                        }
                    }
                }
            }

            return 0;
        }

        private bool TryParseDecimal(string? value, out decimal result)
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
