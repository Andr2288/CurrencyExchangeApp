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
    /// Універсальний адаптер з підтримкою числових валютних кодів (ISO 4217)
    /// </summary>
    public class UniversalApiAdapter : IExchangeRateAdapter
    {
        private readonly HttpClient _httpClient;
        private readonly IRepository<Currency> _currencyRepository;
        private readonly IRepository<ApiSource> _apiSourceRepository;
        private readonly IRepository<ExchangeRate> _exchangeRateRepository;
        private readonly ILogger<UniversalApiAdapter> _logger;
        private readonly ApiSource _apiSource;

        // Мапінг числових кодів ISO 4217 на літерні
        private static readonly Dictionary<int, string> IsoCurrencyCodeMapping = new()
        {
            { 840, "USD" },   // Долар США
            { 978, "EUR" },   // Євро  
            { 980, "UAH" },   // Гривня
            { 985, "PLN" },   // Польський злотий
            { 826, "GBP" },   // Британський фунт
            { 756, "CHF" },   // Швейцарський франк
            { 392, "JPY" },   // Японська єна
            { 156, "CNY" },   // Китайський юань
            { 124, "CAD" },   // Канадський долар
            { 36, "AUD" },    // Австралійський долар
            { 752, "SEK" },   // Шведська крона
            { 578, "NOK" },   // Норвезька крона
            { 208, "DKK" },   // Данська крона
            { 203, "CZK" },   // Чеська крона
            { 348, "HUF" },   // Угорський форинт
            { 946, "RON" },   // Румунський лей
            { 975, "BGN" },   // Болгарський лев
            { 191, "HRK" },   // Хорватська куна
            { 949, "TRY" },   // Турецька ліра
        };

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

                // Додаємо затримку для API з лімітами (як Монобанк)
                if (_apiSource.Name.Contains("моно", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(1000); // 1 секунда затримки
                    _logger.LogDebug("Applied rate limiting delay for {Source}", _apiSource.Name);
                }

                var response = await _httpClient.GetStringAsync(_apiSource.Url);
                _logger.LogInformation("{Source} API response received, length: {Length}", _apiSource.Name, response.Length);

                if (_apiSource.Format.ToUpper() == "JSON")
                {
                    rates = await ParseJsonResponse(response);
                }
                else
                {
                    _logger.LogWarning("Unsupported format: {Format} for source {Source}", _apiSource.Format, _apiSource.Name);
                }

                _logger.LogInformation("Successfully processed {Count} rates from {Source}", rates.Count, _apiSource.Name);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                _logger.LogWarning("Rate limit exceeded for {Source}. Consider increasing interval.", _apiSource.Name);
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

            var currencies = await _currencyRepository.GetAllAsync();
            var uah = currencies.FirstOrDefault(c => c.Code == "UAH");

            if (uah == null)
            {
                _logger.LogError("UAH currency not found in database");
                return rates;
            }

            _logger.LogInformation("Available currencies in DB: {Currencies}", string.Join(", ", currencies.Select(c => c.Code)));

            try
            {
                var jsonDocument = JsonDocument.Parse(jsonResponse);
                _logger.LogInformation("JSON parsed successfully, root type: {Type}", jsonDocument.RootElement.ValueKind);

                if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Processing array with {Count} elements", jsonDocument.RootElement.GetArrayLength());

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
                    _logger.LogInformation("Processing single object");

                    var foundArray = false;
                    foreach (var property in jsonDocument.RootElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            _logger.LogInformation("Found array property: {PropertyName} with {Count} elements", property.Name, property.Value.GetArrayLength());

                            foreach (var element in property.Value.EnumerateArray())
                            {
                                var rate = await TryParseRateElement(element, currencies, uah);
                                if (rate != null)
                                {
                                    rates.Add(rate);
                                }
                            }
                            foundArray = true;
                        }
                    }

                    if (!foundArray)
                    {
                        var rate = await TryParseRateElement(jsonDocument.RootElement, currencies, uah);
                        if (rate != null)
                        {
                            rates.Add(rate);
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON response from {Source}", _apiSource.Name);
            }

            _logger.LogInformation("Parsed {Count} valid rates from {Source}", rates.Count, _apiSource.Name);
            return rates;
        }

        private async Task<ExchangeRate?> TryParseRateElement(JsonElement element, IEnumerable<Currency> currencies, Currency uah)
        {
            try
            {
                _logger.LogDebug("Parsing rate element: {Element}", element.GetRawText());

                string? currencyCode = null;
                decimal buyRate = 0;
                decimal sellRate = 0;

                // Спочатку пробуємо числові коди (для Монобанку та інших)
                if (TryParseNumericCurrencyCode(element, out currencyCode))
                {
                    _logger.LogDebug("Found numeric currency code: {Code}", currencyCode);
                }
                else
                {
                    // Якщо числовий код не знайдено, шукаємо літерні
                    var currencyFields = new[] {
                        "ccy", "cc", "currency", "currencyCode", "from", "code",
                        "currency_code", "baseCurrency", "base", "symbol"
                    };

                    foreach (var field in currencyFields)
                    {
                        if (element.TryGetProperty(field, out var prop))
                        {
                            currencyCode = prop.GetString();
                            _logger.LogDebug("Found string currency code '{Code}' in field '{Field}'", currencyCode, field);
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(currencyCode))
                {
                    _logger.LogDebug("No currency code found in element: {Element}", element.GetRawText());
                    return null;
                }

                // Знаходимо валюту в нашій БД
                var currency = currencies.FirstOrDefault(c => c.Code.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));
                if (currency == null)
                {
                    _logger.LogDebug("Currency '{Code}' not found in database", currencyCode);
                    return null;
                }

                // Різні поля для курсів (включаючи Монобанк)
                var buyFields = new[] { "rateBuy", "buy", "buyRate", "bid", "rate", "value", "buy_rate" };
                var sellFields = new[] { "rateSell", "sell", "sellRate", "ask", "rate", "value", "sell_rate", "sale" };
                var crossFields = new[] { "rateCross", "cross", "rate", "value" }; // Для кроскурсів

                buyRate = TryGetDecimalValue(element, buyFields);
                sellRate = TryGetDecimalValue(element, sellFields);

                // Якщо немає buy/sell, пробуємо cross курс
                if (buyRate == 0 && sellRate == 0)
                {
                    var crossRate = TryGetDecimalValue(element, crossFields);
                    if (crossRate > 0)
                    {
                        buyRate = sellRate = crossRate;
                        _logger.LogDebug("Using cross rate for {Currency}: {Rate}", currencyCode, crossRate);
                    }
                }

                _logger.LogDebug("Extracted rates for {Currency}: buy={Buy}, sell={Sell}", currencyCode, buyRate, sellRate);

                // М'яка валідація
                if (buyRate <= 0 && sellRate <= 0)
                {
                    _logger.LogWarning("All rates are zero for {Currency}", currencyCode);
                    return null;
                }

                // Використовуємо наявний курс для обох напрямків
                if (buyRate <= 0) buyRate = sellRate;
                if (sellRate <= 0) sellRate = buyRate;

                // Перевірка на UAH -> UAH (пропускаємо)
                if (currency.Code == "UAH")
                {
                    _logger.LogDebug("Skipping UAH to UAH rate");
                    return null;
                }

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

                try
                {
                    await _exchangeRateRepository.AddAsync(exchangeRate);
                    await _exchangeRateRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Saved rate for {Currency}: buy={Buy}, sell={Sell}",
                        currencyCode, buyRate, sellRate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save rate for {Currency} to database", currencyCode);
                    return null;
                }

                return exchangeRate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing rate element from {Source}", _apiSource.Name);
                return null;
            }
        }

        /// <summary>
        /// Парсить числові валютні коди (ISO 4217) як у Монобанку
        /// </summary>
        private bool TryParseNumericCurrencyCode(JsonElement element, out string? currencyCode)
        {
            currencyCode = null;

            // Шукаємо currencyCodeA (основна валюта) та currencyCodeB (базова валюта)
            if (element.TryGetProperty("currencyCodeA", out var codeAProp) &&
                element.TryGetProperty("currencyCodeB", out var codeBProp))
            {
                if (codeAProp.TryGetInt32(out int codeA) && codeBProp.TryGetInt32(out int codeB))
                {
                    // Якщо currencyCodeB = 980 (UAH), то currencyCodeA - це валюта до UAH
                    if (codeB == 980 && IsoCurrencyCodeMapping.TryGetValue(codeA, out currencyCode))
                    {
                        _logger.LogDebug("Mapped numeric code {NumericCode} to {CurrencyCode}", codeA, currencyCode);
                        return true;
                    }
                }
            }

            return false;
        }

        private decimal TryGetDecimalValue(JsonElement element, string[] possibleFields)
        {
            foreach (var field in possibleFields)
            {
                if (element.TryGetProperty(field, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Number)
                    {
                        if (prop.TryGetDecimal(out var decimalValue))
                        {
                            return decimalValue;
                        }
                    }
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
            if (string.IsNullOrWhiteSpace(value)) return false;

            var formats = new[]
            {
                CultureInfo.InvariantCulture,
                CultureInfo.GetCultureInfo("uk-UA"),
                CultureInfo.GetCultureInfo("en-US")
            };

            foreach (var culture in formats)
            {
                if (decimal.TryParse(value, NumberStyles.Number, culture, out result))
                    return true;
            }

            // Нормалізація розділювачів
            var normalizedValue = value.Replace(',', '.');
            if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
                return true;

            normalizedValue = value.Replace('.', ',');
            if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.GetCultureInfo("uk-UA"), out result))
                return true;

            return false;
        }
    }
}
