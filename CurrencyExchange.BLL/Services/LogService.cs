using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.Services
{
    public class LogService
    {
        private readonly IRepository<SystemLog> _logRepository;

        public LogService(IRepository<SystemLog> logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task LogAsync(string level, string source, string message)
        {
            var log = new SystemLog
            {
                Level = level,
                Source = source,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await _logRepository.AddAsync(log);
            await _logRepository.SaveChangesAsync();
        }

        public async Task<List<SystemLog>> GetLatestLogsAsync(int count = 50, string? level = null)
        {
            var logs = await _logRepository.GetAllAsync();

            var query = logs.AsQueryable();

            // Фільтрація за рівнем якщо параметр задано
            if (!string.IsNullOrEmpty(level))
            {
                query = query.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderByDescending(l => l.Timestamp)
                       .Take(count)
                       .ToList();
        }

        /// <summary>
        /// Видалити логи старше 30 днів
        /// </summary>
        public async Task<int> CleanupOldLogsAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            var oldLogs = await _logRepository.FindAsync(l => l.Timestamp < cutoffDate);

            if (oldLogs.Any())
            {
                foreach (var log in oldLogs)
                {
                    await _logRepository.DeleteAsync(log); // Передаємо об'єкт, не ID
                }

                await _logRepository.SaveChangesAsync();
                return oldLogs.Count();
            }

            return 0;
        }
    }
}
