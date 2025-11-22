using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CurrencyExchange.DAL.Models;

namespace CurrencyExchange.BLL.Interfaces
{
    public interface ICurrencyService
    {
        Task<IEnumerable<Currency>> GetAllCurrenciesAsync();
        Task<Currency?> GetCurrencyByIdAsync(int id);
        Task<Currency?> GetCurrencyByCodeAsync(string code);
        Task<Currency> CreateCurrencyAsync(Currency currency);
        Task UpdateCurrencyAsync(Currency currency);
        Task DeleteCurrencyAsync(int id);
    }
}
