using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyExchange.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiSourcesController : ControllerBase
    {
        private readonly IRepository<ApiSource> _apiSourceRepository;

        public ApiSourcesController(IRepository<ApiSource> apiSourceRepository)
        {
            _apiSourceRepository = apiSourceRepository;
        }

        /// <summary>
        /// Отримати всі банки/джерела API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sources = await _apiSourceRepository.GetAllAsync();
            return Ok(sources.Select(s => new
            {
                s.Id,
                s.Name,
                s.Url,
                s.IsActive,
                s.UpdateIntervalMinutes,
                s.LastUpdateAt
            }));
        }

        /// <summary>
        /// Отримати тільки активні банки
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var sources = await _apiSourceRepository.FindAsync(s => s.IsActive);
            return Ok(sources.Select(s => new
            {
                s.Id,
                s.Name
            }));
        }
    }
}
