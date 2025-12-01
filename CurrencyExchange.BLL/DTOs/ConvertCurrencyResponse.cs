using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.DTOs
{
    public class ConvertCurrencyResponse
    {
        public string FromCurrencyCode { get; set; } = string.Empty;
        public string ToCurrencyCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public DateTime RateDate { get; set; }

        /// <summary>
        /// Тип використаного курсу
        /// </summary>
        public ConversionType UsedConversionType { get; set; }

        /// <summary>
        /// Деталі курсів для повної інформації
        /// </summary>
        public ExchangeRateDetails RateDetails { get; set; } = new ExchangeRateDetails();
    }

    public class ExchangeRateDetails
    {
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
        public decimal AverageRate { get; set; }

        /// <summary>
        /// Чи використовувався зворотний курс
        /// </summary>
        public bool IsReverseConversion { get; set; }

        /// <summary>
        /// Чи використовувалася конвертація через UAH
        /// </summary>
        public bool IsViaUahConversion { get; set; }

        /// <summary>
        /// Опис логіки конвертації для користувача
        /// </summary>
        public string ConversionPath { get; set; } = string.Empty;
    }
}
