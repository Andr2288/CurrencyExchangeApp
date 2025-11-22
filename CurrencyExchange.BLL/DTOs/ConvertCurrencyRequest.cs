using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.DTOs
{
    public class ConvertCurrencyRequest
    {
        public string FromCurrencyCode { get; set; } = string.Empty;
        public string ToCurrencyCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? SourceName { get; set; } // Опціонально: якщо хочемо вибрати банк
    }
}
