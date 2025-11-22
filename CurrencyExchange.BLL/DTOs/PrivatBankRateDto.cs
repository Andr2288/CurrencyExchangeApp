using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.DTOs
{
    // ПриватБанк API response
    public class PrivatBankRateDto
    {
        public string ccy { get; set; } = string.Empty;      // Валюта (USD, EUR)
        public string base_ccy { get; set; } = string.Empty; // Базова валюта (UAH)
        public string buy { get; set; } = string.Empty;      // Курс купівлі
        public string sale { get; set; } = string.Empty;     // Курс продажу
    }
}
