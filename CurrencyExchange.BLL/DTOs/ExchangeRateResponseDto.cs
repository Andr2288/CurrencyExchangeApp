using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.DTOs
{
    public class ExchangeRateResponseDto
    {
        public int Id { get; set; }

        // Валюти
        public string FromCurrencyCode { get; set; } = string.Empty;
        public string FromCurrencyName { get; set; } = string.Empty;
        public string FromCurrencySymbol { get; set; } = string.Empty;

        public string ToCurrencyCode { get; set; } = string.Empty;
        public string ToCurrencyName { get; set; } = string.Empty;
        public string ToCurrencySymbol { get; set; } = string.Empty;

        // Джерело
        public string SourceName { get; set; } = string.Empty;

        // Курси
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }

        // Час
        public DateTime FetchedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
