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
                    RateDate = DateTime.UtcNow,
                    UsedConversionType = request.ConversionType,
                    RateDetails = new ExchangeRateDetails
                    {
                        BuyRate = 1,
                        SellRate = 1,
                        AverageRate = 1,
                        ConversionPath = "Same currency"
                    }
                };
            }

            // Спочатку шукаємо прямий курс
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
                var rate = GetRateByType(latestDirectRate, request.ConversionType);
                var convertedAmount = request.Amount * rate;

                return new ConvertCurrencyResponse
                {
                    FromCurrencyCode = fromCurrency.Code,
                    ToCurrencyCode = toCurrency.Code,
                    Amount = request.Amount,
                    ConvertedAmount = Math.Round(convertedAmount, 2),
                    ExchangeRate = rate,
                    SourceName = latestDirectRate.ApiSource?.Name ?? "Unknown",
                    RateDate = latestDirectRate.FetchedAt,
                    UsedConversionType = request.ConversionType,
                    RateDetails = new ExchangeRateDetails
                    {
                        BuyRate = latestDirectRate.BuyRate,
                        SellRate = latestDirectRate.SellRate,
                        AverageRate = (latestDirectRate.BuyRate + latestDirectRate.SellRate) / 2,
                        ConversionPath = $"Direct: {fromCurrency.Code} → {toCurrency.Code}"
                    }
                };
            }

            // Якщо прямого курсу немає, шукаємо зворотний курс
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
                // Для зворотного курсу логіка інша:
                // Якщо користувач хоче купити USD за UAH, він платить по курсу продажу банку
                // Якщо користувач хоче продати USD за UAH, він отримує по курсу купівлі банку
                var reverseRate = GetReverseRateByType(latestReverseRate, request.ConversionType);
                var convertedAmount = request.Amount * reverseRate;

                return new ConvertCurrencyResponse
                {
                    FromCurrencyCode = fromCurrency.Code,
                    ToCurrencyCode = toCurrency.Code,
                    Amount = request.Amount,
                    ConvertedAmount = Math.Round(convertedAmount, 4),
                    ExchangeRate = Math.Round(reverseRate, 6),
                    SourceName = latestReverseRate.ApiSource?.Name ?? "Unknown",
                    RateDate = latestReverseRate.FetchedAt,
                    UsedConversionType = request.ConversionType,
                    RateDetails = new ExchangeRateDetails
                    {
                        BuyRate = Math.Round(1 / latestReverseRate.SellRate, 6),
                        SellRate = Math.Round(1 / latestReverseRate.BuyRate, 6),
                        AverageRate = Math.Round(1 / ((latestReverseRate.BuyRate + latestReverseRate.SellRate) / 2), 6),
                        IsReverseConversion = true,
                        ConversionPath = $"Reverse: {toCurrency.Code} → {fromCurrency.Code} (inverted)"
                    }
                };
            }

            // Логіка через UAH (якщо обидві валюти не UAH)
            var uahCurrency = currencies.FirstOrDefault(c => c.Code == "UAH");
            if (uahCurrency != null && fromCurrency.Code != "UAH" && toCurrency.Code != "UAH")
            {
                _logger.LogDebug("Trying conversion through UAH: {From} -> UAH -> {To}", request.FromCurrencyCode, request.ToCurrencyCode);

                var fromToUahRates = await _exchangeRateRepository.GetRatesByCurrencyPairAsync(fromCurrency.Id, uahCurrency.Id);
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
                    // From -> UAH
                    var fromToUahExchangeRate = GetRateByType(fromToUahRate, request.ConversionType);
                    var amountInUah = request.Amount * fromToUahExchangeRate;

                    // UAH -> To (зворотний курс)
                    var uahToToExchangeRate = GetReverseRateByType(uahToToRate, request.ConversionType);
                    var finalAmount = amountInUah * uahToToExchangeRate;

                    // Комбінований курс
                    var combinedRate = fromToUahExchangeRate * uahToToExchangeRate;

                    return new ConvertCurrencyResponse
                    {
                        FromCurrencyCode = fromCurrency.Code,
                        ToCurrencyCode = toCurrency.Code,
                        Amount = request.Amount,
                        ConvertedAmount = Math.Round(finalAmount, 4),
                        ExchangeRate = Math.Round(combinedRate, 6),
                        SourceName = $"{fromToUahRate.ApiSource?.Name ?? "Unknown"}",
                        RateDate = new[] { fromToUahRate.FetchedAt, uahToToRate.FetchedAt }.Min(),
                        UsedConversionType = request.ConversionType,
                        RateDetails = new ExchangeRateDetails
                        {
                            BuyRate = Math.Round(GetRateByType(fromToUahRate, ConversionType.Buy) * GetReverseRateByType(uahToToRate, ConversionType.Buy), 6),
                            SellRate = Math.Round(GetRateByType(fromToUahRate, ConversionType.Sell) * GetReverseRateByType(uahToToRate, ConversionType.Sell), 6),
                            AverageRate = Math.Round(combinedRate, 6),
                            IsViaUahConversion = true,
                            ConversionPath = $"Via UAH: {fromCurrency.Code} → UAH → {toCurrency.Code}"
                        }
                    };
                }
            }

            // Якщо нічого не знайшли
            _logger.LogWarning("No exchange rate found for {From} -> {To} (tried direct, reverse, and via UAH)", request.FromCurrencyCode, request.ToCurrencyCode);
            return null;
        }

        /// <summary>
        /// Отримує курс за типом для прямого курсу
        /// </summary>
        private decimal GetRateByType(ExchangeRate rate, ConversionType conversionType)
        {
            return conversionType switch
            {
                ConversionType.Buy => rate.BuyRate,
                ConversionType.Sell => rate.SellRate,
                ConversionType.Average => (rate.BuyRate + rate.SellRate) / 2,
                _ => rate.SellRate // За замовчуванням
            };
        }

        /// <summary>
        /// Отримує зворотний курс за типом
        /// </summary>
        private decimal GetReverseRateByType(ExchangeRate rate, ConversionType conversionType)
        {
            // Для зворотного курсу логіка інвертована:
            // Buy стає Sell і навпаки
            return conversionType switch
            {
                ConversionType.Buy => 1 / rate.SellRate,   // Коли купуємо, використовуємо 1/SellRate
                ConversionType.Sell => 1 / rate.BuyRate,   // Коли продаємо, використовуємо 1/BuyRate
                ConversionType.Average => 1 / ((rate.BuyRate + rate.SellRate) / 2),
                _ => 1 / rate.BuyRate // За замовчуванням
            };
        }
    }
}
