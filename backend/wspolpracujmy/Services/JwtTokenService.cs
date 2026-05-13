using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Services
{
    /// <summary>
    /// Serwis generujący tokeny JWT dla użytkowników aplikacji.
    /// </summary>
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public JwtTokenService(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        /// <summary>
        /// Tworzy token JWT na podstawie danych użytkownika i konfiguracji.
        /// </summary>
        public async Task<string> GenerateTokenAsync(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new ArgumentNullException("Brak konfiguracji JwtSettings: 'SecretKey'.");
            var issuer = jwtSettings["Issuer"] ?? "wspolpracujmy";
            var audience = jwtSettings["Audience"] ?? "wspolpracujmy";
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

            var teamId = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == user.Id)
                .Select(s => s.GroupId)
                .FirstOrDefaultAsync();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("fullName", $"{user.Name} {user.Surname}")
            };

            if (teamId.HasValue)
            {
                claims.Add(new Claim("teamId", teamId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}