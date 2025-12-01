using CurrencyExchange.BLL.DTOs;
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
    public class CurrencyConversionService
    {
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly IRepository<Currency> _currencyRepository;
        private readonly ILogger<CurrencyConversionService> _logger;

        public CurrencyConversionService(
            IExchangeRateRepository exchangeRateRepository,
            IRepository<Currency> currencyRepository,
            ILogger<CurrencyConversionService> logger)
        {
            _exchangeRateRepository = exchangeRateRepository;
            _currencyRepository = currencyRepository;
            _logger = logger;
        }

        public async Task<ConvertCurrencyResponse?> ConvertAsync(ConvertCurrencyRequest request)
        {
            // Валідація
            if (request.Amount <= 0)
            {
                _logger.LogWarning("Invalid amount: {Amount}", request.Amount);
                return null;
            }

            // Знаходимо валюти
            var currencies = await _currencyRepository.GetAllAsync();
            var fromCurrency = currencies.FirstOrDefault(c => c.Code == request.FromCurrencyCode.ToUpper());
            var toCurrency = currencies.FirstOrDefault(c => c.Code == request.ToCurrencyCode.ToUpper());

            if (fromCurrency == null || toCurrency == null)
            {
                _logger.LogWarning("Currency not found: {From} -> {To}", request.FromCurrencyCode, request.ToCurrencyCode);
                return null;
            }

            // Якщо та сама валюта
            if (fromCurrency.Id == toCurrency.Id)
            {
                return new ConvertCurrencyResponse
                {
                    FromCurrencyCode = fromCurrency.Code,
                    ToCurrencyCode = toCurrency.Code,
                    Amount = request.Amount,
                    ConvertedAmount = request.Amount,
                    ExchangeRate = 1,
                    SourceName = "Direct",
                    RateDate = DateTime.UtcNow
                };
            }

            // 🔥 ВИПРАВЛЕНО: Спочатку шукаємо прямий курс
            var directRates = await _exchangeRateRepository.GetRatesByCurrencyPairAsync(fromCurrency.Id, toCurrency.Id);

            // Фільтруємо за джерелом якщо вказано
            if (!string.IsNullOrEmpty(request.SourceName))
            {
                directRates = directRates.Where(r => r.ApiSource?.Name == request.SourceName);
            }

            var latestDirectRate = directRates.OrderByDescending(r => r.FetchedAt).FirstOrDefault();

            // Якщо знайшли прямий курс - використовуємо його
            if (latestDirectRate != null)
            {
                var convertedAmount = request.Amount * latestDirectRate.SellRate;

                return new ConvertCurrencyResponse
                {
                    FromCurrencyCode = fromCurrency.Code,
                    ToCurrencyCode = toCurrency.Code,
                    Amount = request.Amount,
                    ConvertedAmount = Math.Round(convertedAmount, 2),
                    ExchangeRate = latestDirectRate.SellRate,
                    SourceName = latestDirectRate.ApiSource?.Name ?? "Unknown",
                    RateDate = latestDirectRate.FetchedAt
                };
            }

            // 🔥 ВИПРАВЛЕНО: Якщо прямого курсу немає, шукаємо зворотний курс
            _logger.LogDebug("Direct rate not found for {From} -> {To}, trying reverse conversion", request.FromCurrencyCode, request.ToCurrencyCode);

            var reverseRates = await _exchangeRateRepository.GetRatesByCurrencyPairAsync(toCurrency.Id, fromCurrency.Id);

            // Фільтруємо за джерелом якщо вказано
            if (!string.IsNullOrEmpty(request.SourceName))
            {
                reverseRates = reverseRates.Where(r => r.ApiSource?.Name == request.SourceName);
            }

            var latestReverseRate = reverseRates.OrderByDescending(r => r.FetchedAt).FirstOrDefault();

            if (latestReverseRate != null)
            {
                // Для зворотного курсу: якщо є USD->UAH = 40, то UAH->USD = 1/40 = 0.025
                // Використовуємо BuyRate для зворотної конвертації (коли банк купує валюту у клієнта)
                var reverseRate = 1 / latestReverseRate.BuyRate;
                var convertedAmount = request.Amount * reverseRate;

                return new ConvertCurrencyResponse
                {
                    FromCurrencyCode = fromCurrency.Code,
                    ToCurrencyCode = toCurrency.Code,
                    Amount = request.Amount,
                    ConvertedAmount = Math.Round(convertedAmount, 4), // Більше знаків для маленьких сум
                    ExchangeRate = Math.Round(reverseRate, 6),
                    SourceName = latestReverseRate.ApiSource?.Name ?? "Unknown",
                    RateDate = latestReverseRate.FetchedAt
                };
            }

            // 🔥 ДОДАНО: Логіка через UAH (якщо обидві валюти не UAH)
            var uahCurrency = currencies.FirstOrDefault(c => c.Code == "UAH");
            if (uahCurrency != null && fromCurrency.Code != "UAH" && toCurrency.Code != "UAH")
            {
                _logger.LogDebug("Trying conversion through UAH: {From} -> UAH -> {To}", request.FromCurrencyCode, request.ToCurrencyCode);

                // Конвертуємо From -> UAH
                var fromToUahRates = await _exchangeRateRepository.GetRatesByCurrencyPairAsync(fromCurrency.Id, uahCurrency.Id);

                // Конвертуємо UAH -> To (зворотний курс To -> UAH)
                var uahToToRates = await _exchangeRateRepository.GetRatesByCurrencyPairAsync(toCurrency.Id, uahCurrency.Id);

                if (!string.IsNullOrEmpty(request.SourceName))
                {
                    fromToUahRates = fromToUahRates.Where(r => r.ApiSource?.Name == request.SourceName);
                    uahToToRates = uahToToRates.Where(r => r.ApiSource?.Name == request.SourceName);
                }

                var fromToUahRate = fromToUahRates.OrderByDescending(r => r.FetchedAt).FirstOrDefault();
                var uahToToRate = uahToToRates.OrderByDescending(r => r.FetchedAt).FirstOrDefault();

                if (fromToUahRate != null && uahToToRate != null)
                {
                    // From -> UAH (продаємо From валюту)
                    var amountInUah = request.Amount * fromToUahRate.SellRate;

                    // UAH -> To (купуємо To валюту) = 1 / BuyRate
                    var uahToToExchangeRate = 1 / uahToToRate.BuyRate;
                    var finalAmount = amountInUah * uahToToExchangeRate;

                    // Комбінований курс
                    var combinedRate = fromToUahRate.SellRate * uahToToExchangeRate;

                    return new ConvertCurrencyResponse
                    {
                        FromCurrencyCode = fromCurrency.Code,
                        ToCurrencyCode = toCurrency.Code,
                        Amount = request.Amount,
                        ConvertedAmount = Math.Round(finalAmount, 4),
                        ExchangeRate = Math.Round(combinedRate, 6),
                        SourceName = $"{fromToUahRate.ApiSource?.Name ?? "Unknown"} (via UAH)",
                        RateDate = new[] { fromToUahRate.FetchedAt, uahToToRate.FetchedAt }.Min()
                    };
                }
            }

            // Якщо нічого не знайшли
            _logger.LogWarning("No exchange rate found for {From} -> {To} (tried direct, reverse, and via UAH)", request.FromCurrencyCode, request.ToCurrencyCode);
            return null;
        }
    }
}
