using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CurrencyExchange.DAL.Data;
using CurrencyExchange.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CurrencyExchange.DAL
{
    public static class DbInitializer
    {
        public static async Task SeedDataAsync(AppDbContext context)
        {
            // Створюємо БД якщо не існує
            await context.Database.EnsureCreatedAsync();

            // Якщо вже є дані - виходимо
            if (await context.Currencies.AnyAsync())
                return;

            // Додаємо валюти
            var currencies = new[]
            {
            new Currency { Code = "UAH", Name = "Українська гривня", Symbol = "₴", IsActive = true },
            new Currency { Code = "USD", Name = "Долар США", Symbol = "$", IsActive = true },
            new Currency { Code = "EUR", Name = "Євро", Symbol = "€", IsActive = true },
            new Currency { Code = "PLN", Name = "Польський злотий", Symbol = "zł", IsActive = true }
        };

            await context.Currencies.AddRangeAsync(currencies);
            await context.SaveChangesAsync();

            // Додаємо джерела API
            var apiSources = new[]
            {
            new ApiSource
            {
                Name = "ПриватБанк",
                Url = "https://api.privatbank.ua/p24api/pubinfo?exchange&coursid=5",
                Format = "JSON",
                IsActive = true,
                UpdateIntervalMinutes = 10
            },
            new ApiSource
            {
                Name = "НБУ",
                Url = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json",
                Format = "JSON",
                IsActive = true,
                UpdateIntervalMinutes = 15
            }
        };

            await context.ApiSources.AddRangeAsync(apiSources);
            await context.SaveChangesAsync();
        }
    }
}
