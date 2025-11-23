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

        public async Task<List<SystemLog>> GetLatestLogsAsync(int count = 50)
        {
            var logs = await _logRepository.GetAllAsync();
            return logs.OrderByDescending(l => l.Timestamp)
                       .Take(count)
                       .ToList();
        }
    }
}
