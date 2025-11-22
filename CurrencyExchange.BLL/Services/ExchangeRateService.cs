using CurrencyExchange.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;

namespace CurrencyExchange.BLL.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly IExchangeRateRepository _exchangeRateRepository;

        public ExchangeRateService(IExchangeRateRepository exchangeRateRepository)
        {
            _exchangeRateRepository = exchangeRateRepository;
        }

        public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync()
        {
            return await _exchangeRateRepository.GetLatestRatesAsync();
        }

        public async Task<IEnumerable<ExchangeRate>> GetRatesBySourceAsync(int apiSourceId)
        {
            return await _exchangeRateRepository.GetRatesBySourceAsync(apiSourceId);
        }

        public async Task<IEnumerable<ExchangeRate>> GetRatesByCurrencyPairAsync(int fromCurrencyId, int toCurrencyId)
        {
            return await _exchangeRateRepository.GetRatesByCurrencyPairAsync(fromCurrencyId, toCurrencyId);
        }

        public async Task<ExchangeRate> CreateExchangeRateAsync(ExchangeRate exchangeRate)
        {
            exchangeRate.CreatedAt = DateTime.UtcNow;
            exchangeRate.FetchedAt = DateTime.UtcNow;

            await _exchangeRateRepository.AddAsync(exchangeRate);
            await _exchangeRateRepository.SaveChangesAsync();

            return exchangeRate;
        }
    }
}
