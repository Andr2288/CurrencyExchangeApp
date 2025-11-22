using CurrencyExchange.BLL.DTOs;
using CurrencyExchange.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyExchange.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversionController : ControllerBase
    {
        private readonly CurrencyConversionService _conversionService;

        public ConversionController(CurrencyConversionService conversionService)
        {
            _conversionService = conversionService;
        }

        /// <summary>
        /// Конвертувати валюту
        /// </summary>
        /// <param name="request">Параметри конвертації</param>
        /// <returns>Результат конвертації</returns>
        [HttpPost]
        public async Task<IActionResult> Convert([FromBody] ConvertCurrencyRequest request)
        {
            var result = await _conversionService.ConvertAsync(request);

            if (result == null)
                return BadRequest("Conversion failed. Check currency codes and amount.");

            return Ok(result);
        }

        /// <summary>
        /// Конвертувати валюту через GET (простіший варіант)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Convert(
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] decimal amount,
            [FromQuery] string? source = null)
        {
            var request = new ConvertCurrencyRequest
            {
                FromCurrencyCode = from,
                ToCurrencyCode = to,
                Amount = amount,
                SourceName = source
            };

            var result = await _conversionService.ConvertAsync(request);

            if (result == null)
                return BadRequest("Conversion failed. Check currency codes and amount.");

            return Ok(result);
        }
    }
}
