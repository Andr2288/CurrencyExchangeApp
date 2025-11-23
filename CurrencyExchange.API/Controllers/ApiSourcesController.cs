using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using Microsoft.AspNetCore.Authorization;
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

        /// <summary>
        /// [ADMIN] Додати новий банк
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ApiSource apiSource)
        {
            apiSource.CreatedAt = DateTime.UtcNow;
            await _apiSourceRepository.AddAsync(apiSource);
            await _apiSourceRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = apiSource.Id }, apiSource);
        }

        /// <summary>
        /// [ADMIN] Оновити банк
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ApiSource apiSource)
        {
            var existing = await _apiSourceRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            existing.Name = apiSource.Name;
            existing.Url = apiSource.Url;
            existing.Format = apiSource.Format;
            existing.IsActive = apiSource.IsActive;
            existing.UpdateIntervalMinutes = apiSource.UpdateIntervalMinutes;

            await _apiSourceRepository.UpdateAsync(existing);
            await _apiSourceRepository.SaveChangesAsync();

            return Ok(existing);
        }

        /// <summary>
        /// [ADMIN] Видалити банк
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _apiSourceRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _apiSourceRepository.DeleteAsync(existing);
            await _apiSourceRepository.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// [ADMIN] Активувати/деактивувати банк
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var existing = await _apiSourceRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            existing.IsActive = !existing.IsActive;
            await _apiSourceRepository.UpdateAsync(existing);
            await _apiSourceRepository.SaveChangesAsync();

            return Ok(new { id = existing.Id, isActive = existing.IsActive });
        }
    }
}
