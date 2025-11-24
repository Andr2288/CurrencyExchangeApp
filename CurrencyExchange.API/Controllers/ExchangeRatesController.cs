using CurrencyExchange.BLL.DTOs;
using CurrencyExchange.BLL.Services;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyExchange.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeRatesController : ControllerBase
    {
        private readonly ExchangeRateFetchService _fetchService; // Legacy
        private readonly DynamicExchangeRateFetchService _dynamicFetchService; // NEW
        private readonly BLL.Interfaces.IExchangeRateService _exchangeRateService;
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly IRepository<Currency> _currencyRepository;
        private readonly IRepository<ApiSource> _apiSourceRepository;

        public ExchangeRatesController(
            ExchangeRateFetchService fetchService,
            DynamicExchangeRateFetchService dynamicFetchService,
            BLL.Interfaces.IExchangeRateService exchangeRateService,
            IExchangeRateRepository exchangeRateRepository,
            IRepository<Currency> currencyRepository,
            IRepository<ApiSource> apiSourceRepository)
        {
            _fetchService = fetchService;
            _dynamicFetchService = dynamicFetchService;
            _exchangeRateService = exchangeRateService;
            _exchangeRateRepository = exchangeRateRepository;
            _currencyRepository = currencyRepository;
            _apiSourceRepository = apiSourceRepository;
        }

        /// <summary>
        /// Отримати всі останні курси з пагінацією
        /// GET /api/ExchangeRates/latest?page=1&pageSize=10
        /// </summary>
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestRates(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var rates = await _exchangeRateService.GetLatestRatesAsync();
            var ratesList = rates.ToList();

            var totalCount = ratesList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var paginatedRates = ratesList
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var response = new PaginatedResponse<ExchangeRateResponseDto>
            {
                Data = paginatedRates.Select(r => new ExchangeRateResponseDto
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
                }),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            };

            return Ok(response);
        }

        /// <summary>
        /// Отримати курси з фільтрацією та пагінацією
        /// GET /api/ExchangeRates/filter?bank=ПриватБанк&from=USD&to=UAH&page=1&pageSize=10
        /// </summary>
        [HttpGet("filter")]
        public async Task<IActionResult> GetFiltered(
            [FromQuery] string bank = "",
            [FromQuery] string from = "",
            [FromQuery] string to = "",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var rates = await _exchangeRateRepository.GetLatestRatesAsync();

            // Фільтр за банком
            if (!string.IsNullOrEmpty(bank))
            {
                rates = rates.Where(r => r.ApiSource?.Name.Contains(bank, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            // Фільтр за валютою FROM
            if (!string.IsNullOrEmpty(from))
            {
                rates = rates.Where(r => r.FromCurrency?.Code.Equals(from, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            // Фільтр за валютою TO
            if (!string.IsNullOrEmpty(to))
            {
                rates = rates.Where(r => r.ToCurrency?.Code.Equals(to, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            var ratesList = rates.ToList();
            var totalCount = ratesList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var paginatedRates = ratesList
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var response = new PaginatedResponse<ExchangeRateResponseDto>
            {
                Data = paginatedRates.Select(r => new ExchangeRateResponseDto
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
                }),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            };

            return Ok(response);
        }

        /// <summary>
        /// [ADMIN] Ручне оновлення всіх курсів (NEW: Dynamic)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("fetch")]
        public async Task<IActionResult> FetchAllRates([FromQuery] bool useDynamic = true)
        {
            try
            {
                int count;

                if (useDynamic)
                {
                    // NEW: Використовуємо динамічну систему
                    count = await _dynamicFetchService.FetchAllRatesAsync();
                }
                else
                {
                    // LEGACY: Старий метод для зворотної сумісності
                    count = await _fetchService.FetchAllRatesAsync();
                }

                return Ok(new
                {
                    message = $"Fetched {count} rates using {(useDynamic ? "dynamic" : "legacy")} system",
                    count,
                    system = useDynamic ? "dynamic" : "legacy"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error fetching rates",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// [ADMIN] Ручне оновлення курсів з конкретного джерела (NEW: Dynamic)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("fetch/{sourceName}")]
        public async Task<IActionResult> FetchRatesBySource(string sourceName, [FromQuery] bool useDynamic = true)
        {
            try
            {
                int count;

                if (useDynamic)
                {
                    // NEW: Використовуємо динамічну систему
                    count = await _dynamicFetchService.FetchBySourceAsync(sourceName);
                }
                else
                {
                    // LEGACY: Старий метод
                    count = await _fetchService.FetchBySourceAsync(sourceName);
                }

                return Ok(new
                {
                    message = $"Fetched {count} rates from {sourceName} using {(useDynamic ? "dynamic" : "legacy")} system",
                    count,
                    source = sourceName,
                    system = useDynamic ? "dynamic" : "legacy"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Error fetching rates from {sourceName}",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// [ADMIN] Отримати список доступних джерел (NEW)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("sources/available")]
        public async Task<IActionResult> GetAvailableSources()
        {
            try
            {
                var sources = await _dynamicFetchService.GetAvailableSourcesAsync();
                return Ok(new { sources, count = sources.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error getting available sources",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// [ADMIN] Тест нового API джерела (NEW)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("test-source")]
        public async Task<IActionResult> TestApiSource([FromBody] ApiSource testSource)
        {
            try
            {
                // Створюємо тимчасовий універсальний адаптер для тесту
                using var scope = HttpContext.RequestServices.CreateScope();

                var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
                var currencyRepository = scope.ServiceProvider.GetRequiredService<IRepository<Currency>>();
                var apiSourceRepository = scope.ServiceProvider.GetRequiredService<IRepository<ApiSource>>();
                var exchangeRateRepository = scope.ServiceProvider.GetRequiredService<IRepository<ExchangeRate>>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<CurrencyExchange.BLL.Adapters.UniversalApiAdapter>>();

                var adapter = new CurrencyExchange.BLL.Adapters.UniversalApiAdapter(
                    httpClient,
                    currencyRepository,
                    apiSourceRepository,
                    exchangeRateRepository,
                    logger,
                    testSource
                );

                var rates = await adapter.FetchRatesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Test successful! Found {rates.Count} rates",
                    ratesCount = rates.Count,
                    sourceName = testSource.Name,
                    sourceUrl = testSource.Url
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = $"Test failed: {ex.Message}",
                    error = ex.ToString()
                });
            }
        }

        // Existing methods remain the same...
        [HttpGet("source/{apiSourceId}")]
        public async Task<IActionResult> GetRatesBySource(int apiSourceId)
        {
            var rates = await _exchangeRateService.GetRatesBySourceAsync(apiSourceId);
            return Ok(rates.Select(r => new ExchangeRateResponseDto
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
            }));
        }
    }
}
