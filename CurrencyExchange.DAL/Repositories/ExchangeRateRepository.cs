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

        // Варіант 1: Простий (для невеликої кількості записів)
        public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync()
        {
            // Завантажуємо всі курси з navigation properties
            var allRates = await _dbSet
                .Include(er => er.FromCurrency)
                .Include(er => er.ToCurrency)
                .Include(er => er.ApiSource)
                .ToListAsync();

            // Групуємо в пам'яті і беремо останній курс для кожної пари
            return allRates
                .GroupBy(er => new { er.FromCurrencyId, er.ToCurrencyId, er.ApiSourceId })
                .Select(g => g.OrderByDescending(er => er.FetchedAt).First())
                .OrderByDescending(er => er.FetchedAt)
                .ToList();
        }

        // Варіант 2: Оптимізований (якщо багато записів)
        // Раскоментуй цей метод і замінь GetLatestRatesAsync вище
        /*
        public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync()
        {
            // Raw SQL запит для отримання останніх курсів
            var sql = @"
                WITH LatestRates AS (
                    SELECT 
                        er.*,
                        ROW_NUMBER() OVER (
                            PARTITION BY er.""FromCurrencyId"", er.""ToCurrencyId"", er.""ApiSourceId"" 
                            ORDER BY er.""FetchedAt"" DESC
                        ) AS rn
                    FROM ""ExchangeRates"" er
                )
                SELECT * FROM LatestRates WHERE rn = 1
                ORDER BY ""FetchedAt"" DESC";

            var rateIds = await _context.ExchangeRates
                .FromSqlRaw(sql)
                .Select(er => er.Id)
                .ToListAsync();

            // Завантажуємо повні об'єкти з navigation properties
            return await _dbSet
                .Include(er => er.FromCurrency)
                .Include(er => er.ToCurrency)
                .Include(er => er.ApiSource)
                .Where(er => rateIds.Contains(er.Id))
                .OrderByDescending(er => er.FetchedAt)
                .ToListAsync();
        }
        */

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
    }
}
