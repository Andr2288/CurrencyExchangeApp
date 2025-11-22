using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.DAL.Models
{
    public class Currency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(3)]
        public string Code { get; set; } = string.Empty; // USD, EUR, UAH, PLN

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty; // Dollar, Euro, Hryvnia

        [StringLength(10)]
        public string Symbol { get; set; } = string.Empty; // $, €, ₴

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<ExchangeRate> ExchangeRatesFrom { get; set; } = new List<ExchangeRate>();
        public ICollection<ExchangeRate> ExchangeRatesTo { get; set; } = new List<ExchangeRate>();
    }
}
