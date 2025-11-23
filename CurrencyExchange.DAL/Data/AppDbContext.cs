using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using CurrencyExchange.DAL.Models;

namespace CurrencyExchange.DAL.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Currency> Currencies { get; set; }
        public DbSet<ApiSource> ApiSources { get; set; }
        public DbSet<ExchangeRate> ExchangeRates { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Налаштування відносин для ExchangeRate
            modelBuilder.Entity<ExchangeRate>()
                .HasOne(er => er.FromCurrency)
                .WithMany(c => c.ExchangeRatesFrom)
                .HasForeignKey(er => er.FromCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExchangeRate>()
                .HasOne(er => er.ToCurrency)
                .WithMany(c => c.ExchangeRatesTo)
                .HasForeignKey(er => er.ToCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExchangeRate>()
                .HasOne(er => er.ApiSource)
                .WithMany(api => api.ExchangeRates)
                .HasForeignKey(er => er.ApiSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Індекси для оптимізації запитів
            modelBuilder.Entity<Currency>()
                .HasIndex(c => c.Code)
                .IsUnique();

            modelBuilder.Entity<ExchangeRate>()
                .HasIndex(er => new { er.FromCurrencyId, er.ToCurrencyId, er.ApiSourceId, er.FetchedAt });

            // Індекси для User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
