using CurrencyExchange.BLL.Validators;
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
        private readonly ApiSourceValidator _validator;

        public ApiSourcesController(IRepository<ApiSource> apiSourceRepository, ApiSourceValidator validator)
        {
            _apiSourceRepository = apiSourceRepository;
            _validator = validator;
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
                s.Format,
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
        /// [ADMIN] Додати новий банк з валідацією структури API
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ApiSource apiSource)
        {
            // Валідація основних полів
            if (string.IsNullOrWhiteSpace(apiSource.Name) ||
                string.IsNullOrWhiteSpace(apiSource.Url))
            {
                return BadRequest(new
                {
                    message = "Обов'язкові поля: Name та Url"
                });
            }

            // Валідація структури API
            var validationResult = await _validator.ValidateApiSourceAsync(apiSource);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Структура API не відповідає шаблону для автоматичного додавання",
                    errors = validationResult.Errors,
                    hint = "API повинно повертати JSON з масивом валютних курсів"
                });
            }

            // Перевірка унікальності URL
            var existingByUrl = await _apiSourceRepository.FindAsync(s => s.Url == apiSource.Url);
            if (existingByUrl.Any())
            {
                return BadRequest(new
                {
                    message = "API з таким URL вже існує"
                });
            }

            apiSource.CreatedAt = DateTime.UtcNow;
            await _apiSourceRepository.AddAsync(apiSource);
            await _apiSourceRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = apiSource.Id }, new
            {
                message = "API джерело успішно додано та перевірено",
                apiSource.Id,
                apiSource.Name,
                apiSource.Url
            });
        }

        /// <summary>
        /// [ADMIN] Тестування API без збереження
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("test")]
        public async Task<IActionResult> TestApi([FromBody] ApiSource testSource)
        {
            var validationResult = await _validator.ValidateApiSourceAsync(testSource);

            if (validationResult.IsValid)
            {
                return Ok(new
                {
                    success = true,
                    message = "API успішно протестовано та відповідає шаблону",
                    source = testSource.Name
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "API не пройшло валідацію",
                errors = validationResult.Errors
            });
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
                return NotFound(new { message = "API джерело не знайдено" });

            // Валідація якщо змінився URL
            if (existing.Url != apiSource.Url)
            {
                var validationResult = await _validator.ValidateApiSourceAsync(apiSource);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Новий URL не відповідає шаблону",
                        errors = validationResult.Errors
                    });
                }
            }

            existing.Name = apiSource.Name;
            existing.Url = apiSource.Url;
            existing.Format = apiSource.Format;
            existing.IsActive = apiSource.IsActive;
            existing.UpdateIntervalMinutes = apiSource.UpdateIntervalMinutes;

            await _apiSourceRepository.UpdateAsync(existing);
            await _apiSourceRepository.SaveChangesAsync();

            return Ok(new { message = "API джерело оновлено" });
        }

        /// <summary>
        /// [ADMIN] Видалити банк
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var source = await _apiSourceRepository.GetByIdAsync(id);
            if (source == null)
                return NotFound(new { message = "API джерело не знайдено" });

            await _apiSourceRepository.DeleteAsync(source);
            await _apiSourceRepository.SaveChangesAsync();

            return Ok(new { message = "API джерело видалено" });
        }
    }
}
