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
            return Ok(rates);
        }

        // Отримати курси за джерелом
        [HttpGet("source/{apiSourceId}")]
        public async Task<IActionResult> GetRatesBySource(int apiSourceId)
        {
            var rates = await _exchangeRateService.GetRatesBySourceAsync(apiSourceId);
            return Ok(rates);
        }

        // Отримати курси за валютною парою
        [HttpGet("pair/{fromCurrencyId}/{toCurrencyId}")]
        public async Task<IActionResult> GetRatesByCurrencyPair(int fromCurrencyId, int toCurrencyId)
        {
            var rates = await _exchangeRateService.GetRatesByCurrencyPairAsync(fromCurrencyId, toCurrencyId);
            return Ok(rates);
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
