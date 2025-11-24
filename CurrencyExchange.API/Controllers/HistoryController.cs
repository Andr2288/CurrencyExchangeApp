using CurrencyExchange.BLL.DTOs;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyExchange.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : ControllerBase
    {
        private readonly IExchangeRateRepository _exchangeRateRepository;

        public HistoryController(IExchangeRateRepository exchangeRateRepository)
        {
            _exchangeRateRepository = exchangeRateRepository;
        }

        /// <summary>
        /// Отримати курси за сьогодні
        /// GET /api/History/today?from=USD&to=UAH
        /// </summary>
        [HttpGet("today")]
        public async Task<IActionResult> GetToday(
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] string? source = null)
        {
            var endDate = DateTime.UtcNow;
            var startDate = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);

            var rates = await _exchangeRateRepository.GetHistoricalRatesAsync(startDate, endDate);
            var result = ProcessRates(rates, from, to, source, "hour");

            return Ok(new
            {
                Period = "today",
                Count = result.Count,
                Data = result
            });
        }

        /// <summary>
        /// Отримати курси за тиждень
        /// GET /api/History/week?from=USD&to=UAH
        /// </summary>
        [HttpGet("week")]
        public async Task<IActionResult> GetWeek(
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] string? source = null)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-7);

            var rates = await _exchangeRateRepository.GetHistoricalRatesAsync(startDate, endDate);
            var result = ProcessRates(rates, from, to, source, "day");

            return Ok(new
            {
                Period = "week",
                Days = 7,
                Count = result.Count,
                Data = result
            });
        }

        /// <summary>
        /// Отримати курси за місяць
        /// GET /api/History/month?from=USD&to=UAH
        /// </summary>
        [HttpGet("month")]
        public async Task<IActionResult> GetMonth(
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] string? source = null)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-1);

            var rates = await _exchangeRateRepository.GetHistoricalRatesAsync(startDate, endDate);
            var result = ProcessRates(rates, from, to, source, "day");

            return Ok(new
            {
                Period = "month",
                Days = 30,
                Count = result.Count,
                Data = result
            });
        }

        /// <summary>
        /// Отримати останній курс
        /// GET /api/History/latest?from=USD&to=UAH
        /// </summary>
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest(
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] string? source = null)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-1);

            var rates = await _exchangeRateRepository.GetHistoricalRatesAsync(startDate, endDate);

            var filtered = rates.Where(r =>
                r.FromCurrency.Code == from.ToUpper() &&
                r.ToCurrency.Code == to.ToUpper()
            );

            if (!string.IsNullOrEmpty(source))
                filtered = filtered.Where(r => r.ApiSource.Name == source);

            var latest = filtered.OrderByDescending(r => r.FetchedAt).FirstOrDefault();

            if (latest == null)
                return NotFound();

            return Ok(new
            {
                From = latest.FromCurrency.Code,
                To = latest.ToCurrency.Code,
                Source = latest.ApiSource.Name,
                Buy = latest.BuyRate,
                Sell = latest.SellRate,
                Date = latest.FetchedAt
            });
        }

        /// <summary>
        /// Порівняти курси між датами
        /// GET /api/History/compare?from=USD&to=UAH&date1=2024-11-20&date2=2024-11-24
        /// </summary>
        [HttpGet("compare")]
        public async Task<IActionResult> Compare(
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] DateTime date1,
            [FromQuery] DateTime date2)
        {
            var rates1 = await GetRatesForDate(date1, from, to);
            var rates2 = await GetRatesForDate(date2, from, to);

            if (!rates1.Any() || !rates2.Any())
                return NotFound();

            var r1 = rates1.First();
            var r2 = rates2.First();

            var buyChange = r2.BuyRate - r1.BuyRate;
            var sellChange = r2.SellRate - r1.SellRate;

            return Ok(new
            {
                From = from.ToUpper(),
                To = to.ToUpper(),
                Date1 = new { Date = date1, Buy = r1.BuyRate, Sell = r1.SellRate },
                Date2 = new { Date = date2, Buy = r2.BuyRate, Sell = r2.SellRate },
                Change = new
                {
                    Buy = Math.Round(buyChange, 4),
                    Sell = Math.Round(sellChange, 4),
                    BuyPercent = Math.Round((buyChange / r1.BuyRate) * 100, 2),
                    SellPercent = Math.Round((sellChange / r1.SellRate) * 100, 2),
                    Trend = buyChange > 0 ? "up" : (buyChange < 0 ? "down" : "stable")
                }
            });
        }

        // КОМПАКТНА обробка даних
        private List<object> ProcessRates(IEnumerable<ExchangeRate> rates, string? from, string? to, string? source, string groupBy)
        {
            var filtered = rates.AsEnumerable();

            if (!string.IsNullOrEmpty(from))
                filtered = filtered.Where(r => r.FromCurrency.Code == from.ToUpper());

            if (!string.IsNullOrEmpty(to))
                filtered = filtered.Where(r => r.ToCurrency.Code == to.ToUpper());

            if (!string.IsNullOrEmpty(source))
                filtered = filtered.Where(r => r.ApiSource.Name == source);

            // Агрегація
            IEnumerable<ExchangeRate> aggregated;

            if (groupBy == "hour")
            {
                aggregated = filtered
                    .GroupBy(r => new
                    {
                        Hour = new DateTime(r.FetchedAt.Year, r.FetchedAt.Month, r.FetchedAt.Day, r.FetchedAt.Hour, 0, 0),
                        r.FromCurrencyId,
                        r.ToCurrencyId,
                        r.ApiSourceId
                    })
                    .Select(g => g.OrderByDescending(r => r.FetchedAt).First());
            }
            else // day
            {
                aggregated = filtered
                    .GroupBy(r => new
                    {
                        Day = r.FetchedAt.Date,
                        r.FromCurrencyId,
                        r.ToCurrencyId,
                        r.ApiSourceId
                    })
                    .Select(g => g.OrderByDescending(r => r.FetchedAt).First());
            }

            // КОМПАКТНИЙ JSON (тільки потрібні поля)
            var result = new List<object>();
            var grouped = aggregated
                .GroupBy(r => new { r.FromCurrencyId, r.ToCurrencyId, r.ApiSourceId })
                .ToList();

            foreach (var group in grouped)
            {
                var ratesList = group.OrderBy(r => r.FetchedAt).ToList();

                for (int i = 0; i < ratesList.Count; i++)
                {
                    var curr = ratesList[i];

                    decimal? buyChange = null;
                    decimal? sellChange = null;
                    string trend = "stable";

                    if (i > 0)
                    {
                        var prev = ratesList[i - 1];
                        buyChange = Math.Round(curr.BuyRate - prev.BuyRate, 4);
                        sellChange = Math.Round(curr.SellRate - prev.SellRate, 4);
                        trend = buyChange > 0 ? "up" : (buyChange < 0 ? "down" : "stable");
                    }

                    result.Add(new
                    {
                        From = curr.FromCurrency.Code,
                        To = curr.ToCurrency.Code,
                        Source = curr.ApiSource.Name,
                        Buy = curr.BuyRate,
                        Sell = curr.SellRate,
                        Change = buyChange,
                        Trend = trend,
                        Date = curr.FetchedAt.ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }

            return result;
        }

        // Helper
        private async Task<List<ExchangeRate>> GetRatesForDate(DateTime date, string from, string to)
        {
            var start = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var end = DateTime.SpecifyKind(date.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

            var rates = await _exchangeRateRepository.GetHistoricalRatesAsync(start, end);

            return rates
                .Where(r => r.FromCurrency.Code == from.ToUpper() && r.ToCurrency.Code == to.ToUpper())
                .OrderByDescending(r => r.FetchedAt)
                .ToList();
        }
    }
}
