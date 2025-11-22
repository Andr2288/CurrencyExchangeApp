using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.DTOs
{
    // НБУ API response
    public class NbuRateDto
    {
        public int r030 { get; set; }                        // Код валюти
        public string txt { get; set; } = string.Empty;      // Назва валюти
        public decimal rate { get; set; }                    // Офіційний курс
        public string cc { get; set; } = string.Empty;       // Літерний код (USD, EUR)
        public string exchangedate { get; set; } = string.Empty; // Дата
    }
}
