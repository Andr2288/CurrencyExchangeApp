using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CurrencyExchange.DAL.Data;
using CurrencyExchange.DAL.Models;
using Microsoft.EntityFrameworkCore;

using BCrypt.Net;

namespace CurrencyExchange.DAL
{
    public static class DbInitializer
    {
        public static async Task SeedDataAsync(AppDbContext context)
        {
            // Створюємо БД якщо не існує
            await context.Database.EnsureCreatedAsync();

            // Додаємо валюти якщо їх немає
            if (!await context.Currencies.AnyAsync())
            {
                var currencies = new[]
                {
                    new Currency { Code = "UAH", Name = "Українська гривня", Symbol = "₴", IsActive = true },
                    new Currency { Code = "USD", Name = "Долар США", Symbol = "$", IsActive = true },
                    new Currency { Code = "EUR", Name = "Євро", Symbol = "€", IsActive = true },
                    new Currency { Code = "PLN", Name = "Польський злотий", Symbol = "zł", IsActive = true }
                };

                await context.Currencies.AddRangeAsync(currencies);
                await context.SaveChangesAsync();
            }

            // Додаємо джерела API якщо їх немає
            if (!await context.ApiSources.AnyAsync())
            {
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

            // Додаємо користувачів якщо їх немає
            if (!await context.Users.AnyAsync())
            {
                var users = new[]
                {
                    new User
                    {
                        Username = "admin",
                        Email = "admin@currencyexchange.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                        Role = "Admin",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        Username = "testuser",
                        Email = "user@test.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                        Role = "User",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        Username = "john_doe",
                        Email = "john.doe@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                        Role = "User",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        Username = "jane_smith",
                        Email = "jane.smith@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePass456!"),
                        Role = "User",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Users.AddRangeAsync(users);
                await context.SaveChangesAsync();
            }
        }
    }
}
