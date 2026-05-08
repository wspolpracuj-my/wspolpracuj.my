using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs.Auth;
using wspolpracujmy.Models;
using wspolpracujmy.Services;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>
    /// Kontroler uwierzytelniania i autoryzacji użytkowników.
    /// </summary>
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtTokenService _jwtTokenService;

        public AuthController(AppDbContext context, JwtTokenService jwtTokenService)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            // Sprawdź czy login już istnieje
            if (await _context.Users.AnyAsync(u => u.Login == request.Login))
            {
                return BadRequest(new { message = "Login już istnieje" });
            }

            if (request.Role == Role.Student && await _context.Students.AnyAsync(s => s.Email == request.Email))
            {
                return BadRequest(new { message = "E-mail studenta już istnieje w systemie" });
            }

            // If registering as a Student, enforce university email pattern: 6 digits@ g.elearn... or 6 digits@ stud....
            if (request.Role == Role.Student)
            {
                var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
                var pattern = "^\\d{6}@(g\\.elearn\\.uz\\.zgora\\.pl|stud\\.uz\\.zgora\\.pl)$";
                if (!Regex.IsMatch(email, pattern))
                {
                    return BadRequest(new { message = "E-mail studenta musi składać się z 6 cyfr i kończyć się @g.elearn.uz.zgora.pl lub @stud.uz.zgora.pl" });
                }
            }

            // Hashowanie hasła
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var user = new User
            {
                Name = request.Name,
                Surname = request.Surname,
                Login = request.Login,
                PasswordHash = passwordHash,
                Role = request.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (user.Role == Role.Student)
            {
                var student = new Student
                {
                    UserId = user.Id,
                    GroupId = null,
                    Email = request.Email,
                    User = user
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            // Generuj token
            var token = _jwtTokenService.GenerateToken(user);

            var response = new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Login = user.Login,
                FullName = $"{user.Name} {user.Surname}",
                Role = user.Role
            };

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == request.Login);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Nieprawidłowy login lub hasło" });
            }

            var token = _jwtTokenService.GenerateToken(user);

            var response = new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Login = user.Login,
                FullName = $"{user.Name} {user.Surname}",
                Role = user.Role
            };

            return Ok(response);
        }
    }
}