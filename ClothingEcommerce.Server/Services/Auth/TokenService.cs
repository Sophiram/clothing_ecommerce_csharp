using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebApplication_ClothingEcommerce.Models;

namespace ClothingEcommerce.Server.Services.Auth
{
    public interface ITokenService
    {
        string GenerateJwtToken(ApplicationUser user, IList<string> roles);
        ClaimsPrincipal? ValidateToken(string token);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var jwtKey = _configuration["JWT_SECRET"]
                ?? _configuration["Jwt:Key"]
                ?? "ClothingEcommerceSuperSecretKey2026!MustBeAtLeast32BytesLong!";
            var issuer = _configuration["Jwt:Issuer"] ?? "ClothingEcommerce.Server";
            var audience = _configuration["Jwt:Audience"] ?? "ClothingEcommerce.Client";
            var expiryDays = int.TryParse(_configuration["Jwt:ExpiryDays"], out var days) ? days : 7;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                new("firstName", user.FirstName ?? string.Empty),
                new("lastName", user.LastName ?? string.Empty)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expiryDays),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var jwtKey = _configuration["JWT_SECRET"]
                ?? _configuration["Jwt:Key"]
                ?? "ClothingEcommerceSuperSecretKey2026!MustBeAtLeast32BytesLong!";
            var issuer = _configuration["Jwt:Issuer"] ?? "ClothingEcommerce.Server";
            var audience = _configuration["Jwt:Audience"] ?? "ClothingEcommerce.Client";

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
