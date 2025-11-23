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
        private readonly ExchangeRateFetchService _fetchService;
        private readonly BLL.Interfaces.IExchangeRateService _exchangeRateService;
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly IRepository<Currency> _currencyRepository;
        private readonly IRepository<ApiSource> _apiSourceRepository;

        public ExchangeRatesController(
            ExchangeRateFetchService fetchService,
            BLL.Interfaces.IExchangeRateService exchangeRateService,
            IExchangeRateRepository exchangeRateRepository,
            IRepository<Currency> currencyRepository,
            IRepository<ApiSource> apiSourceRepository)
        {
            _fetchService = fetchService;
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
            if (pageSize > 100) pageSize = 100; // Максимум 100 на сторінку

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
        /// Отримати ВСІ курси з історією (пагінація)
        /// GET /api/ExchangeRates/all?page=1&pageSize=50
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllRates(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            // Завантажуємо ВСІ курси з navigation properties
            var allRates = await _exchangeRateRepository.GetAllWithDetailsAsync();
            var ratesList = allRates.ToList();

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
        /// Отримати курси за джерелом (ID)
        /// </summary>
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

        /// <summary>
        /// Отримати курси за валютною парою
        /// </summary>
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

        /// <summary>
        /// [ADMIN] Ручне оновлення всіх курсів
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("fetch")]
        public async Task<IActionResult> FetchAllRates()
        {
            var count = await _fetchService.FetchAllRatesAsync();
            return Ok(new { message = $"Fetched {count} rates", count });
        }

        /// <summary>
        /// [ADMIN] Ручне оновлення курсів з конкретного джерела
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("fetch/{sourceName}")]
        public async Task<IActionResult> FetchRatesBySource(string sourceName)
        {
            var count = await _fetchService.FetchRatesBySourceAsync(sourceName);
            return Ok(new { message = $"Fetched {count} rates from {sourceName}", count });
        }
    }
}
