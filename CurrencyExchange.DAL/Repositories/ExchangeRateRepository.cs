using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using CurrencyExchange.DAL.Data;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;

namespace CurrencyExchange.DAL.Repositories
{
    public class ExchangeRateRepository : Repository<ExchangeRate>, IExchangeRateRepository
    {
        public ExchangeRateRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync()
        {
            var allRates = await _dbSet
                .Include(er => er.FromCurrency)
                .Include(er => er.ToCurrency)
                .Include(er => er.ApiSource)
                .ToListAsync();

            return allRates
                .GroupBy(er => new { er.FromCurrencyId, er.ToCurrencyId, er.ApiSourceId })
                .Select(g => g.OrderByDescending(er => er.FetchedAt).First())
                .OrderByDescending(er => er.FetchedAt)
                .ToList();
        }

        public async Task<IEnumerable<ExchangeRate>> GetRatesBySourceAsync(int apiSourceId)
        {
            return await _dbSet
                .Include(er => er.FromCurrency)
                .Include(er => er.ToCurrency)
                .Include(er => er.ApiSource)
                .Where(er => er.ApiSourceId == apiSourceId)
                .OrderByDescending(er => er.FetchedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExchangeRate>> GetRatesByCurrencyPairAsync(int fromCurrencyId, int toCurrencyId)
        {
            return await _dbSet
                .Include(er => er.FromCurrency)
                .Include(er => er.ToCurrency)
                .Include(er => er.ApiSource)
                .Where(er => er.FromCurrencyId == fromCurrencyId && er.ToCurrencyId == toCurrencyId)
                .OrderByDescending(er => er.FetchedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExchangeRate>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(er => er.FromCurrency)
                .Include(er => er.ToCurrency)
                .Include(er => er.ApiSource)
                .OrderByDescending(er => er.FetchedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Отримати історичні дані за період
        /// </summary>
        public async Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(
            DateTime startDate,
            DateTime endDate,
            int? fromCurrencyId = null,
            int? toCurrencyId = null,
            int? apiSourceId = null)
        {
            var query = _dbSet
                .Include(er => er.FromCurrency)
                .Include(er => er.ToCurrency)
                .Include(er => er.ApiSource)
                .Where(er => er.FetchedAt >= startDate && er.FetchedAt <= endDate);

            if (fromCurrencyId.HasValue)
                query = query.Where(er => er.FromCurrencyId == fromCurrencyId.Value);

            if (toCurrencyId.HasValue)
                query = query.Where(er => er.ToCurrencyId == toCurrencyId.Value);

            if (apiSourceId.HasValue)
                query = query.Where(er => er.ApiSourceId == apiSourceId.Value);

            return await query
                .OrderByDescending(er => er.FetchedAt)
                .ToListAsync();
        }
    }
}
