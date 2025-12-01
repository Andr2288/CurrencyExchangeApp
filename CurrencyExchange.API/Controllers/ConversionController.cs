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
        /// Конвертувати валюту через GET (розширена версія)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Convert(
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] decimal amount,
            [FromQuery] string? source = null,
            [FromQuery] string type = "sell") // buy, sell, average
        {
            // Парсимо тип конвертації
            var conversionType = type.ToLower() switch
            {
                "buy" => ConversionType.Buy,
                "sell" => ConversionType.Sell,
                "average" => ConversionType.Average,
                _ => ConversionType.Sell
            };

            var request = new ConvertCurrencyRequest
            {
                FromCurrencyCode = from,
                ToCurrencyCode = to,
                Amount = amount,
                SourceName = source,
                ConversionType = conversionType
            };

            var result = await _conversionService.ConvertAsync(request);

            if (result == null)
                return BadRequest("Conversion failed. Check currency codes and amount.");

            return Ok(result);
        }

        /// <summary>
        /// Отримати всі доступні курси для валютної пари
        /// GET /api/Conversion/rates?from=USD&to=UAH&source=ПриватБанк
        /// </summary>
        [HttpGet("rates")]
        public async Task<IActionResult> GetRates(
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] string? source = null)
        {
            try
            {
                // Створюємо запити для всіх типів курсів
                var buyRequest = new ConvertCurrencyRequest
                {
                    FromCurrencyCode = from,
                    ToCurrencyCode = to,
                    Amount = 1, // Для отримання курсу
                    SourceName = source,
                    ConversionType = ConversionType.Buy
                };

                var sellRequest = new ConvertCurrencyRequest
                {
                    FromCurrencyCode = from,
                    ToCurrencyCode = to,
                    Amount = 1,
                    SourceName = source,
                    ConversionType = ConversionType.Sell
                };

                var averageRequest = new ConvertCurrencyRequest
                {
                    FromCurrencyCode = from,
                    ToCurrencyCode = to,
                    Amount = 1,
                    SourceName = source,
                    ConversionType = ConversionType.Average
                };

                var buyResult = await _conversionService.ConvertAsync(buyRequest);
                var sellResult = await _conversionService.ConvertAsync(sellRequest);
                var averageResult = await _conversionService.ConvertAsync(averageRequest);

                if (buyResult == null || sellResult == null || averageResult == null)
                {
                    return BadRequest("No rates found for the specified currency pair.");
                }

                return Ok(new
                {
                    FromCurrency = from.ToUpper(),
                    ToCurrency = to.ToUpper(),
                    Source = buyResult.SourceName,
                    RateDate = buyResult.RateDate,
                    Rates = new
                    {
                        Buy = new
                        {
                            Rate = buyResult.ExchangeRate,
                            Description = "Курс купівлі (банк купує валюту у клієнта)"
                        },
                        Sell = new
                        {
                            Rate = sellResult.ExchangeRate,
                            Description = "Курс продажу (банк продає валюту клієнту)"
                        },
                        Average = new
                        {
                            Rate = averageResult.ExchangeRate,
                            Description = "Середній курс"
                        }
                    },
                    ConversionPath = buyResult.RateDetails.ConversionPath,
                    IsReverse = buyResult.RateDetails.IsReverseConversion,
                    IsViaUah = buyResult.RateDetails.IsViaUahConversion
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting rates: {ex.Message}");
            }
        }

        /// <summary>
        /// Пояснення типів курсів
        /// GET /api/Conversion/help
        /// </summary>
        [HttpGet("help")]
        public IActionResult GetConversionHelp()
        {
            return Ok(new
            {
                ConversionTypes = new
                {
                    Buy = new
                    {
                        Code = "buy",
                        Name = "Курс купівлі",
                        Description = "Курс, за яким банк купує валюту у клієнта",
                        Example = "Ви продаєте $100 банку за курсом купівлі",
                        WhenToUse = "Коли ви міняєте іноземну валюту на гривню"
                    },
                    Sell = new
                    {
                        Code = "sell",
                        Name = "Курс продажу",
                        Description = "Курс, за яким банк продає валюту клієнту",
                        Example = "Ви купуєте $100 у банку за курсом продажу",
                        WhenToUse = "Коли ви купуєте іноземну валюту за гривню"
                    },
                    Average = new
                    {
                        Code = "average",
                        Name = "Середній курс",
                        Description = "Середнє арифметичне між курсами купівлі та продажу",
                        Example = "Buy: 40.00, Sell: 40.50 → Average: 40.25",
                        WhenToUse = "Для довідкових цілей або прогнозування"
                    }
                },
                Examples = new
                {
                    BasicConversion = "/api/Conversion?from=USD&to=UAH&amount=100&type=sell",
                    BuyConversion = "/api/Conversion?from=UAH&to=USD&amount=4000&type=buy",
                    SpecificBank = "/api/Conversion?from=EUR&to=UAH&amount=50&type=sell&source=ПриватБанк",
                    GetAllRates = "/api/Conversion/rates?from=USD&to=UAH"
                }
            });
        }
    }
}
