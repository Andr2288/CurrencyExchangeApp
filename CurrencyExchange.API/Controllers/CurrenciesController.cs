using Microsoft.AspNetCore.Mvc;

using CurrencyExchange.BLL.Interfaces;
using CurrencyExchange.DAL.Models;

namespace CurrencyExchange.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrenciesController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;

        public CurrenciesController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Currency>>> GetAll()
        {
            var currencies = await _currencyService.GetAllCurrenciesAsync();
            return Ok(currencies);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Currency>> GetById(int id)
        {
            var currency = await _currencyService.GetCurrencyByIdAsync(id);

            if (currency == null)
                return NotFound();

            return Ok(currency);
        }

        [HttpGet("code/{code}")]
        public async Task<ActionResult<Currency>> GetByCode(string code)
        {
            var currency = await _currencyService.GetCurrencyByCodeAsync(code);

            if (currency == null)
                return NotFound();

            return Ok(currency);
        }

        [HttpPost]
        public async Task<ActionResult<Currency>> Create(Currency currency)
        {
            var created = await _currencyService.CreateCurrencyAsync(currency);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
    }
}
