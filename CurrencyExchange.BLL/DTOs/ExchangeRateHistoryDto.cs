using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.DTOs
{
    public class ExchangeRateHistoryDto
    {
        public int Id { get; set; }
        public string FromCurrencyCode { get; set; } = string.Empty;
        public string FromCurrencyName { get; set; } = string.Empty;
        public string FromCurrencySymbol { get; set; } = string.Empty;
        public string ToCurrencyCode { get; set; } = string.Empty;
        public string ToCurrencyName { get; set; } = string.Empty;
        public string ToCurrencySymbol { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
        public DateTime FetchedAt { get; set; }

        // Зміни курсу
        public decimal? BuyRateChange { get; set; }
        public decimal? SellRateChange { get; set; }
        public decimal? BuyRateChangePercent { get; set; }
        public decimal? SellRateChangePercent { get; set; }
        public string BuyRateTrend { get; set; } = "stable"; // up, down, stable
        public string SellRateTrend { get; set; } = "stable"; // up, down, stable
    }
}
