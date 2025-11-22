using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.DAL.Models
{
    public class ApiSource
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // ПриватБанк, НБУ

        [Required]
        [StringLength(500)]
        public string Url { get; set; } = string.Empty; // API endpoint

        [StringLength(50)]
        public string Format { get; set; } = "JSON"; // JSON, XML

        public bool IsActive { get; set; } = true;

        public int UpdateIntervalMinutes { get; set; } = 10; // Інтервал оновлення

        public DateTime? LastUpdateAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<ExchangeRate> ExchangeRates { get; set; } = new List<ExchangeRate>();
    }
}
