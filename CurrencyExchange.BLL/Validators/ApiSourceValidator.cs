using CurrencyExchange.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.Validators
{
    public class ApiSourceValidator
    {
        private readonly HttpClient _httpClient;

        public ApiSourceValidator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ValidationResult> ValidateApiSourceAsync(ApiSource apiSource)
        {
            var result = new ValidationResult { IsValid = true };

            // 1. Валідація URL
            if (!Uri.IsWellFormedUriString(apiSource.Url, UriKind.Absolute))
            {
                result.IsValid = false;
                result.Errors.Add("URL має неправильний формат");
                return result;
            }

            // 2. Валідація формату
            if (string.IsNullOrWhiteSpace(apiSource.Format) ||
                !apiSource.Format.Equals("JSON", StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Errors.Add("Підтримується тільки JSON формат");
                return result;
            }

            // 3. Тестування доступності API
            try
            {
                var response = await _httpClient.GetStringAsync(apiSource.Url);

                // 4. Валідація JSON структури
                var validation = ValidateJsonStructure(response);
                if (!validation.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(validation.Errors);
                }
            }
            catch (HttpRequestException)
            {
                result.IsValid = false;
                result.Errors.Add("API недоступне за вказаним URL");
            }
            catch (TaskCanceledException)
            {
                result.IsValid = false;
                result.Errors.Add("Таймаут підключення до API");
            }

            return result;
        }

        private ValidationResult ValidateJsonStructure(string jsonResponse)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                var jsonDocument = JsonDocument.Parse(jsonResponse);
                var rootElement = jsonDocument.RootElement;

                // Перевірка структури для курсів валют
                if (!IsValidCurrencyStructure(rootElement))
                {
                    result.IsValid = false;
                    result.Errors.Add("JSON не містить валідної структури курсів валют");
                    result.Errors.Add("Очікується: масив з об'єктами, що мають поля для валюти та курсів");
                }
            }
            catch (JsonException)
            {
                result.IsValid = false;
                result.Errors.Add("Відповідь API не є валідним JSON");
            }

            return result;
        }

        private bool IsValidCurrencyStructure(JsonElement rootElement)
        {
            // Перевірка чи є масив в корені або всередині об'єкта
            if (rootElement.ValueKind == JsonValueKind.Array)
            {
                return HasValidCurrencyFields(rootElement);
            }

            if (rootElement.ValueKind == JsonValueKind.Object)
            {
                // Шукаємо масив всередині об'єкта
                foreach (var property in rootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        return HasValidCurrencyFields(property.Value);
                    }
                }
            }

            return false;
        }

        private bool HasValidCurrencyFields(JsonElement arrayElement)
        {
            if (arrayElement.GetArrayLength() == 0) return false;

            var firstItem = arrayElement[0];
            if (firstItem.ValueKind != JsonValueKind.Object) return false;

            // Перевірка наявності полів для валюти
            var hasBaseCurrency = false;
            var hasRateFields = false;

            foreach (var property in firstItem.EnumerateObject())
            {
                var name = property.Name.ToLower();

                // Поля валюти (різні варіанти назв)
                if (name.Contains("ccy") || name.Contains("currency") ||
                    name.Contains("code") || name.Contains("currencycodea") ||
                    name.Contains("cc"))
                {
                    hasBaseCurrency = true;
                }

                // Поля курсів (різні варіанти назв) 
                if (name.Contains("buy") || name.Contains("sale") ||
                    name.Contains("rate") || name.Contains("ratebuy") ||
                    name.Contains("ratesell") || name.Contains("ratecross"))
                {
                    hasRateFields = true;
                }
            }

            return hasBaseCurrency && hasRateFields;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
