using CurrencyExchange.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyExchange.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class LogsController : ControllerBase
    {
        private readonly LogService _logService;

        public LogsController(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// [ADMIN] Отримати останні логи системи
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLatestLogs([FromQuery] int count = 50)
        {
            if (count < 1) count = 50;
            if (count > 200) count = 200;

            var logs = await _logService.GetLatestLogsAsync(count);

            return Ok(logs.Select(l => new
            {
                l.Id,
                l.Level,
                l.Source,
                l.Message,
                l.Timestamp
            }));
        }
    }
}
