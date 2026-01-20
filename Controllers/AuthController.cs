using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IConfiguration _configuration;

        public AuthController(IMongoDatabase mongoDatabase, IConfiguration configuration)
        {
            //var database = mongoClient.GetDatabase("IoTDatabase");
            //_usersCollection = database.GetCollection<User>("Users");

            var collectionName = "Users";  // Debe coincidir con appsettings
            _usersCollection = mongoDatabase.GetCollection<User>(collectionName);
            _configuration = configuration;

        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new LoginResponse { Success = false, Message = "Email and password are required." });
            }

            try
            {
                // Check if email exists
                var existingUser = await _usersCollection.Find(Builders<User>.Filter.Eq(u => u.Email, request.Email.ToLowerInvariant())).FirstOrDefaultAsync();
                if (existingUser != null)
                {
                    return BadRequest(new LoginResponse { Success = false, Message = "Email already exists." });
                }

                var user = new User
                {
                    Email = request.Email.ToLowerInvariant(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
                };

                await _usersCollection.InsertOneAsync(user);
                return Ok(new LoginResponse { Success = true, Message = "User registered successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Register error: {ex.Message}");
                return StatusCode(500, new LoginResponse { Success = false, Message = "Internal server error." });
            }
        }

        /// <summary>
        /// Authenticates a user by email and password.
        /// </summary>
        /// <param name="request">Login request with email and password.</param>
        /// <returns>Login response indicating success or failure.</returns>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new LoginResponse { Success = false, Message = "Email and password are required." });
            }

            try
            {
                // Find user by email (case-insensitive for simplicity; adjust if needed)
                var filter = Builders<User>.Filter.Eq(u => u.Email, request.Email.ToLowerInvariant());
                var user = await _usersCollection.Find(filter).FirstOrDefaultAsync();

                if (user == null)
                {
                    return Unauthorized(new LoginResponse { Success = false, Message = "Invalid email or password." });
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                
                if (!isPasswordValid)
                {
                    return Unauthorized(new LoginResponse { Success = false, Message = "Invalid email or password." });
                }

                // Generate JWT token
                var token = GenerateJwtToken(user);
                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    Token = token
                });
            }
            catch (Exception ex)
            {
                // Log the exception (use ILogger if injected)
                Console.WriteLine($"Login error: {ex.Message}");
                return StatusCode(500, new LoginResponse { Success = false, Message = "Internal server error." });
            }
        }
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(jwtSettings["TokenExpiryInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
