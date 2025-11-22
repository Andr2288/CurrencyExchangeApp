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
            return await _dbSet
                .OrderByDescending(er => er.FetchedAt)
                .Take(100)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExchangeRate>> GetRatesBySourceAsync(int apiSourceId)
        {
            return await _dbSet
                .Where(er => er.ApiSourceId == apiSourceId)
                .OrderByDescending(er => er.FetchedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExchangeRate>> GetRatesByCurrencyPairAsync(int fromCurrencyId, int toCurrencyId)
        {
            return await _dbSet
                .Where(er => er.FromCurrencyId == fromCurrencyId && er.ToCurrencyId == toCurrencyId)
                .OrderByDescending(er => er.FetchedAt)
                .ToListAsync();
        }
    }
}
