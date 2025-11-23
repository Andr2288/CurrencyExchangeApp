using CurrencyExchange.BLL.DTOs;
using CurrencyExchange.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyExchange.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Реєстрація нового користувача
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request);

            if (response == null)
                return BadRequest("Username or email already exists");

            return Ok(response);
        }

        /// <summary>
        /// Вхід користувача
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);

            if (response == null)
                return Unauthorized("Invalid username or password");

            return Ok(response);
        }
    }
}
