using CurrencyExchange.BLL.DTOs;
using CurrencyExchange.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyExchange.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeRatesController : ControllerBase
    {
        private readonly ExchangeRateFetchService _fetchService;
        private readonly BLL.Interfaces.IExchangeRateService _exchangeRateService;

        public ExchangeRatesController(
            ExchangeRateFetchService fetchService,
            BLL.Interfaces.IExchangeRateService exchangeRateService)
        {
            _fetchService = fetchService;
            _exchangeRateService = exchangeRateService;
        }

        // Отримати всі останні курси
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestRates()
        {
            var rates = await _exchangeRateService.GetLatestRatesAsync();

            var response = rates.Select(r => new ExchangeRateResponseDto
            {
                Id = r.Id,
                FromCurrencyCode = r.FromCurrency?.Code ?? "",
                FromCurrencyName = r.FromCurrency?.Name ?? "",
                FromCurrencySymbol = r.FromCurrency?.Symbol ?? "",
                ToCurrencyCode = r.ToCurrency?.Code ?? "",
                ToCurrencyName = r.ToCurrency?.Name ?? "",
                ToCurrencySymbol = r.ToCurrency?.Symbol ?? "",
                SourceName = r.ApiSource?.Name ?? "",
                BuyRate = r.BuyRate,
                SellRate = r.SellRate,
                FetchedAt = r.FetchedAt,
                CreatedAt = r.CreatedAt
            });

            return Ok(response);
        }

        // Отримати курси за джерелом
        [HttpGet("source/{apiSourceId}")]
        public async Task<IActionResult> GetRatesBySource(int apiSourceId)
        {
            var rates = await _exchangeRateService.GetRatesBySourceAsync(apiSourceId);

            var response = rates.Select(r => new ExchangeRateResponseDto
            {
                Id = r.Id,
                FromCurrencyCode = r.FromCurrency?.Code ?? "",
                FromCurrencyName = r.FromCurrency?.Name ?? "",
                FromCurrencySymbol = r.FromCurrency?.Symbol ?? "",
                ToCurrencyCode = r.ToCurrency?.Code ?? "",
                ToCurrencyName = r.ToCurrency?.Name ?? "",
                ToCurrencySymbol = r.ToCurrency?.Symbol ?? "",
                SourceName = r.ApiSource?.Name ?? "",
                BuyRate = r.BuyRate,
                SellRate = r.SellRate,
                FetchedAt = r.FetchedAt,
                CreatedAt = r.CreatedAt
            });

            return Ok(response);
        }

        // Отримати курси за валютною парою
        [HttpGet("pair/{fromCurrencyId}/{toCurrencyId}")]
        public async Task<IActionResult> GetRatesByCurrencyPair(int fromCurrencyId, int toCurrencyId)
        {
            var rates = await _exchangeRateService.GetRatesByCurrencyPairAsync(fromCurrencyId, toCurrencyId);

            var response = rates.Select(r => new ExchangeRateResponseDto
            {
                Id = r.Id,
                FromCurrencyCode = r.FromCurrency?.Code ?? "",
                FromCurrencyName = r.FromCurrency?.Name ?? "",
                FromCurrencySymbol = r.FromCurrency?.Symbol ?? "",
                ToCurrencyCode = r.ToCurrency?.Code ?? "",
                ToCurrencyName = r.ToCurrency?.Name ?? "",
                ToCurrencySymbol = r.ToCurrency?.Symbol ?? "",
                SourceName = r.ApiSource?.Name ?? "",
                BuyRate = r.BuyRate,
                SellRate = r.SellRate,
                FetchedAt = r.FetchedAt,
                CreatedAt = r.CreatedAt
            });

            return Ok(response);
        }

        // Ручне оновлення всіх курсів
        [HttpPost("fetch")]
        public async Task<IActionResult> FetchAllRates()
        {
            var count = await _fetchService.FetchAllRatesAsync();
            return Ok(new { message = $"Fetched {count} rates", count });
        }

        // Ручне оновлення курсів з конкретного джерела
        [HttpPost("fetch/{sourceName}")]
        public async Task<IActionResult> FetchRatesBySource(string sourceName)
        {
            var count = await _fetchService.FetchRatesBySourceAsync(sourceName);
            return Ok(new { message = $"Fetched {count} rates from {sourceName}", count });
        }
    }
}
