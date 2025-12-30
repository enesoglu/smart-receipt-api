using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using smart_receipt_api.DTOs;
using smart_receipt_api.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace smart_receipt_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _userService = userService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request)
        {
            try
            {
                var errors = new List<string>();

                if (string.IsNullOrEmpty(request.Username))
                    errors.Add("Username is required");
                if (string.IsNullOrEmpty(request.Password))
                    errors.Add("Password is required");
                if (!string.IsNullOrEmpty(request.Password) && request.Password.Length < 6)
                    errors.Add("Password must be at least 6 characters");

                if (errors.Count > 0)
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });

                var user = await _userService.RegisterAsync(request.Username, request.Password);
                if (user == null)
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "User already exists",
                        Errors = new List<string> { "Username is already taken" }
                    });

                var token = GenerateJwtToken(user.Id, user.Username);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Registration successful",
                    Data = new AuthResponse
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        Token = token
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Registration error: {ex.Message}");
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "An error occurred during registration"
                });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
        {
            try
            {
                var errors = new List<string>();

                if (string.IsNullOrEmpty(request.Username))
                    errors.Add("Username is required");
                if (string.IsNullOrEmpty(request.Password))
                    errors.Add("Password is required");

                if (errors.Count > 0)
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });

                var user = await _userService.LoginAsync(request.Username, request.Password);
                if (user == null)
                    return Unauthorized(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });

                var token = GenerateJwtToken(user.Id, user.Username);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new AuthResponse
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        Token = token
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }

        private string GenerateJwtToken(int userId, string username)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? "your-secret-key-must-be-at-least-32-characters-long-here");
            var issuer = jwtSettings["Issuer"] ?? "smart-receipt-api";
            var audience = jwtSettings["Audience"] ?? "smart-receipt-users";
            var expirationMinutes = int.TryParse(jwtSettings["ExpirationMinutes"], out var minutes) ? minutes : 60;

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, username)
                }),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}

