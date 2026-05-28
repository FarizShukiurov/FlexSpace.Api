using FlexSpace.Api.Data;
using FlexSpace.Api.DTOs;
using FlexSpace.Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace FlexSpace.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IValidator<RegisterRequest> _registerValidator;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, IValidator<RegisterRequest> registerValidator)
        {
            _context = context;
            _configuration = configuration;
            _registerValidator = registerValidator;
        }

        /// <summary>
        /// Registers a new customer account in the system.
        /// </summary>
        /// <param name="request">User registration details (email, password, etc.)</param>
        /// <response code="200">Successfully created the account.</response>
        /// <response code="400">Validation error or email already exists.</response>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var validationResult = await _registerValidator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "User with this email already exists." });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful!" });
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        /// <param name="request">User login credentials.</param>
        /// <response code="200">Returns the JWT access token.</response>
        /// <response code="401">Invalid email or password.</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid email or password." });
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return BadRequest(new { message = "Invalid email or password." });
            }

            string token = GenerateJwtToken(user);

            return Ok(new { token = token, role = user.Role, name = user.Name });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role) 
        };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7), 
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
