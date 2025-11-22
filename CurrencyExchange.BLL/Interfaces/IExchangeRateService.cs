using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CurrencyExchange.DAL.Models;

namespace CurrencyExchange.BLL.Interfaces
{
    public interface IExchangeRateService
    {
        Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync();
        Task<IEnumerable<ExchangeRate>> GetRatesBySourceAsync(int apiSourceId);
        Task<IEnumerable<ExchangeRate>> GetRatesByCurrencyPairAsync(int fromCurrencyId, int toCurrencyId);
        Task<ExchangeRate> CreateExchangeRateAsync(ExchangeRate exchangeRate);
    }
}
