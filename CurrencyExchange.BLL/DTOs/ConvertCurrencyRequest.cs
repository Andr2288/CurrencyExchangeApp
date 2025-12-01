using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.DTOs
{
    public enum ConversionType
    {
        /// <summary>
        /// Курс купівлі (банк купує валюту у клієнта)
        /// </summary>
        Buy,

        /// <summary>
        /// Курс продажу (банк продає валюту клієнту)
        /// </summary>
        Sell,

        /// <summary>
        /// Середній курс (середнє між Buy та Sell)
        /// </summary>
        Average
    }

    public class ConvertCurrencyRequest
    {
        [Required]
        public string FromCurrencyCode { get; set; } = string.Empty;

        [Required]
        public string ToCurrencyCode { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Сума повинна бути більше 0")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Опціонально: якщо хочемо вибрати конкретний банк
        /// </summary>
        public string? SourceName { get; set; }

        /// <summary>
        /// Тип конвертації: Buy (купівля), Sell (продаж), Average (середній)
        /// </summary>
        public ConversionType ConversionType { get; set; } = ConversionType.Sell;
    }
}
