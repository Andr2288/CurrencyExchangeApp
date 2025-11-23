using CurrencyExchange.BLL.DTOs;
using CurrencyExchange.DAL.Interfaces;
using CurrencyExchange.DAL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchange.BLL.Services
{
    public class AuthService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IRepository<User> userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            // Перевірка чи існує користувач
            var existingUsers = await _userRepository.FindAsync(u =>
                u.Username == request.Username || u.Email == request.Email);

            if (existingUsers.Any())
                return null;

            // Хешування пароля
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return new AuthResponse
            {
                Token = GenerateJwtToken(user),
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            };
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var users = await _userRepository.FindAsync(u => u.Username == request.Username);
            var user = users.FirstOrDefault();

            if (user == null || !user.IsActive)
                return null;

            // Перевірка пароля
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            // Оновлення LastLoginAt
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return new AuthResponse
            {
                Token = GenerateJwtToken(user),
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            };
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "super-secret-key-minimum-32-characters-long-for-security"));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "CurrencyExchangeAPI",
                audience: _configuration["Jwt:Audience"] ?? "CurrencyExchangeClient",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
