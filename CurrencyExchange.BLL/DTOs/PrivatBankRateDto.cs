using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.DTOs
{
    // ПриватБанк API response
    public class PrivatBankRateDto
    {
        [JsonPropertyName("ccy")]
        public string ccy { get; set; } = string.Empty;      // Валюта (USD, EUR)

        [JsonPropertyName("base_ccy")]
        public string base_ccy { get; set; } = string.Empty; // Базова валюта (UAH)

        [JsonPropertyName("buy")]
        public string buy { get; set; } = string.Empty;      // Курс купівлі

        [JsonPropertyName("sale")]
        public string sale { get; set; } = string.Empty;     // Курс продажу
    }
}
