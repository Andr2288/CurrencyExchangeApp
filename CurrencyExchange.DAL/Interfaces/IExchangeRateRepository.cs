using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CurrencyExchange.DAL.Models;

namespace CurrencyExchange.DAL.Interfaces
{
    public interface IExchangeRateRepository : IRepository<ExchangeRate>
    {
        Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync();
        Task<IEnumerable<ExchangeRate>> GetRatesBySourceAsync(int apiSourceId);
        Task<IEnumerable<ExchangeRate>> GetRatesByCurrencyPairAsync(int fromCurrencyId, int toCurrencyId);
        Task<IEnumerable<ExchangeRate>> GetAllWithDetailsAsync();
        Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(
            DateTime startDate,
            DateTime endDate,
            int? fromCurrencyId = null,
            int? toCurrencyId = null,
            int? apiSourceId = null);
    }
}
