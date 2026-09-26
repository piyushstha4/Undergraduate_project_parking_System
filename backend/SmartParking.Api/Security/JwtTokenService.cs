using System;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartParking.Api.Models;

namespace SmartParking.Api.Security
{
    public static class JwtTokenService
    {
        private static readonly JwtSecurityTokenHandler Handler = new JwtSecurityTokenHandler();

        public static string Secret =>
            ConfigurationManager.AppSettings["JwtSecret"] ?? "smart-parking-dev-secret-change-in-production";

        public static string Create(UserDto user)
        {
            var days = 7;
            int.TryParse(ConfigurationManager.AppSettings["JwtDays"], out days);
            if (days <= 0) days = 7;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: new[]
                {
                    new Claim("id", user.Id.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                    new Claim("role", user.Role ?? string.Empty),
                    new Claim("name", user.Name ?? string.Empty),
                    new Claim("email", user.Email ?? string.Empty)
                },
                expires: DateTime.UtcNow.AddDays(days),
                signingCredentials: credentials);

            return Handler.WriteToken(token);
        }

        public static ClaimsPrincipal Validate(string token)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
            return Handler.ValidateToken(token, parameters, out _);
        }
    }
}
