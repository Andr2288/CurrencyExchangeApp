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

            // Шукаємо курс
            var rates = await _exchangeRateRepository.GetRatesByCurrencyPairAsync(fromCurrency.Id, toCurrency.Id);

            // Фільтруємо за джерелом якщо вказано
            if (!string.IsNullOrEmpty(request.SourceName))
            {
                rates = rates.Where(r => r.ApiSource?.Name == request.SourceName);
            }

            var latestRate = rates.OrderByDescending(r => r.FetchedAt).FirstOrDefault();

            if (latestRate == null)
            {
                _logger.LogWarning("No exchange rate found for {From} -> {To}", request.FromCurrencyCode, request.ToCurrencyCode);
                return null;
            }

            // Конвертуємо (використовуємо SellRate - курс продажу банку)
            var convertedAmount = request.Amount * latestRate.SellRate;

            return new ConvertCurrencyResponse
            {
                FromCurrencyCode = fromCurrency.Code,
                ToCurrencyCode = toCurrency.Code,
                Amount = request.Amount,
                ConvertedAmount = Math.Round(convertedAmount, 2),
                ExchangeRate = latestRate.SellRate,
                SourceName = latestRate.ApiSource?.Name ?? "Unknown",
                RateDate = latestRate.FetchedAt
            };
        }
    }
}
