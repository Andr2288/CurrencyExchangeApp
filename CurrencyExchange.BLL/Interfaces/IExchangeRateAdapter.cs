using CurrencyExchange.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.Interfaces
{
    public interface IExchangeRateAdapter
    {
        Task<List<ExchangeRate>> FetchRatesAsync();
        string GetSourceName();
    }
}
