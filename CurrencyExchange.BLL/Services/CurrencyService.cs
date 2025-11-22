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
    public class CurrencyService : ICurrencyService
    {
        private readonly IRepository<Currency> _currencyRepository;

        public CurrencyService(IRepository<Currency> currencyRepository)
        {
            _currencyRepository = currencyRepository;
        }

        public async Task<IEnumerable<Currency>> GetAllCurrenciesAsync()
        {
            return await _currencyRepository.GetAllAsync();
        }

        public async Task<Currency?> GetCurrencyByIdAsync(int id)
        {
            return await _currencyRepository.GetByIdAsync(id);
        }

        public async Task<Currency?> GetCurrencyByCodeAsync(string code)
        {
            var currencies = await _currencyRepository.FindAsync(c => c.Code == code.ToUpper());
            return currencies.FirstOrDefault();
        }

        public async Task<Currency> CreateCurrencyAsync(Currency currency)
        {
            currency.Code = currency.Code.ToUpper();
            currency.CreatedAt = DateTime.UtcNow;

            await _currencyRepository.AddAsync(currency);
            await _currencyRepository.SaveChangesAsync();

            return currency;
        }

        public async Task UpdateCurrencyAsync(Currency currency)
        {
            await _currencyRepository.UpdateAsync(currency);
            await _currencyRepository.SaveChangesAsync();
        }

        public async Task DeleteCurrencyAsync(int id)
        {
            var currency = await _currencyRepository.GetByIdAsync(id);
            if (currency != null)
            {
                await _currencyRepository.DeleteAsync(currency);
                await _currencyRepository.SaveChangesAsync();
            }
        }
    }
}
