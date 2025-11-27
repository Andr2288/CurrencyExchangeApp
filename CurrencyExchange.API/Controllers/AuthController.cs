using CurrencyExchange.BLL.DTOs;
using CurrencyExchange.BLL.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CurrencyExchange.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IAntiforgery _antiforgery;

        public AuthController(AuthService authService, IAntiforgery antiforgery)
        {
            _authService = authService;
            _antiforgery = antiforgery;
        }

        /// <summary>
        /// Отримати CSRF токен для форм (login/register)
        /// </summary>
        [HttpGet("csrf-token")]
        public IActionResult GetCsrfToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new { csrfToken = tokens.RequestToken });
        }

        /// <summary>
        /// Реєстрація нового користувача (з CSRF захистом)
        /// </summary>
        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request);

            if (response == null)
                return BadRequest("Username or email already exists");

            return Ok(response);
        }

        /// <summary>
        /// Вхід користувача (з CSRF захистом)
        /// </summary>
        [HttpPost("login")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);

            if (response == null)
                return Unauthorized("Invalid username or password");

            return Ok(response);
        }

        /// <summary>
        /// Перевірка авторизації - для frontend
        /// </summary>
        [HttpGet("check")]
        public IActionResult CheckAuth()
        {
            var authHeader = Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Ok(new { isAuthenticated = false });
            }

            return Ok(new { isAuthenticated = false });
        }

        /// <summary>
        /// Отримати дані поточного користувача (потрібен токен)
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                isAuthenticated = true,
                user = new
                {
                    username,
                    email,
                    role
                }
            });
        }

        /// <summary>
        /// Вихід (logout) - просто повертає успіх
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { success = true, message = "Logged out successfully" });
        }
    }
}
