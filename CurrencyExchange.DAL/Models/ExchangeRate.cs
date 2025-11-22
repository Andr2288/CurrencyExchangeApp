using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.DAL.Models
{
    public class ExchangeRate
    {
        [Key]
        public int Id { get; set; }

        // Валюта з якої конвертуємо
        [Required]
        public int FromCurrencyId { get; set; }

        [ForeignKey(nameof(FromCurrencyId))]
        public Currency FromCurrency { get; set; } = null!;

        // Валюта в яку конвертуємо
        [Required]
        public int ToCurrencyId { get; set; }

        [ForeignKey(nameof(ToCurrencyId))]
        public Currency ToCurrency { get; set; } = null!;

        // Джерело курсу
        [Required]
        public int ApiSourceId { get; set; }

        [ForeignKey(nameof(ApiSourceId))]
        public ApiSource ApiSource { get; set; } = null!;

        // Курс купівлі
        [Column(TypeName = "decimal(18,4)")]
        public decimal BuyRate { get; set; }

        // Курс продажу
        [Column(TypeName = "decimal(18,4)")]
        public decimal SellRate { get; set; }

        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
