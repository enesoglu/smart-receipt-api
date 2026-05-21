using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using smart_receipt_api.DTOs;
using smart_receipt_api.Models;
using smart_receipt_api.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace smart_receipt_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Auth")]
    public class AuthController : BaseApiController
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

        /// <summary>Registers a new user with full name, email, and password.</summary>
        /// <param name="request">Registration details.</param>
        /// <response code="200">Registration succeeded.</response>
        /// <response code="400">The request is invalid or the email is already registered.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request)
        {
            try
            {
                var errors = ValidateRegisterRequest(request);
                if (errors.Count > 0)
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Validation failed.",
                        Errors = errors
                    });
                }

                var user = await _userService.RegisterAsync(request.FullName, request.Email, request.Password);
                if (user == null)
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Email is already registered.",
                        Errors = new List<string> { "Email is already registered." }
                    });
                }

                var response = CreateAuthResponse(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Registration successful.",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed.");
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "An error occurred during registration."
                });
            }
        }

        /// <summary>Authenticates a user with email and password.</summary>
        /// <param name="request">Login credentials.</param>
        /// <response code="200">Login succeeded.</response>
        /// <response code="400">The request is invalid.</response>
        /// <response code="401">The credentials are invalid.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
        {
            try
            {
                var errors = ValidateLoginRequest(request);
                if (errors.Count > 0)
                {
                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Validation failed.",
                        Errors = errors
                    });
                }

                var user = await _userService.LoginAsync(request.Email, request.Password);
                if (user == null)
                {
                    return Unauthorized(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    });
                }

                var response = CreateAuthResponse(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Login successful.",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed.");
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "An error occurred during login."
                });
            }
        }

        /// <summary>Gets the current authenticated user's profile.</summary>
        /// <response code="200">The profile was found.</response>
        /// <response code="401">The request is not authenticated.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> Me()
        {
            var userId = GetUserId();
            if (userId == 0)
                return Unauthorized(new ApiResponse<UserProfileDto> { Success = false, Message = "Unauthorized." });

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return Unauthorized(new ApiResponse<UserProfileDto> { Success = false, Message = "Unauthorized." });

            return Ok(new ApiResponse<UserProfileDto>
            {
                Success = true,
                Data = new UserProfileDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                }
            });
        }

        private AuthResponse CreateAuthResponse(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expirationMinutes = int.TryParse(jwtSettings["ExpirationMinutes"], out var minutes) ? minutes : 1440;
            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

            return new AuthResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Token = GenerateJwtToken(user, expiresAt),
                ExpiresAt = expiresAt
            };
        }

        private string GenerateJwtToken(User user, DateTime expiresAt)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? "REPLACE_WITH_AT_LEAST_32_CHAR_RANDOM_SECRET");
            var issuer = jwtSettings["Issuer"] ?? "smart-receipt-api";
            var audience = jwtSettings["Audience"] ?? "smart-receipt-users";

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email)
                }),
                Expires = expiresAt,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static List<string> ValidateRegisterRequest(RegisterRequest request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.FullName))
                errors.Add("Full name is required.");
            else if (request.FullName.Trim().Length is < 2 or > 100)
                errors.Add("Full name must be between 2 and 100 characters.");

            ValidateEmail(request.Email, errors);
            ValidatePassword(request.Password, errors);

            return errors;
        }

        private static List<string> ValidateLoginRequest(LoginRequest request)
        {
            var errors = new List<string>();

            ValidateEmail(request.Email, errors);

            if (string.IsNullOrWhiteSpace(request.Password))
                errors.Add("Password is required.");

            return errors;
        }

        private static void ValidateEmail(string email, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("Email is required.");
                return;
            }

            try
            {
                var address = new MailAddress(email.Trim());
                if (!string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase))
                    errors.Add("Enter a valid email address.");
            }
            catch (FormatException)
            {
                errors.Add("Enter a valid email address.");
            }
        }

        private static void ValidatePassword(string password, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required.");
                return;
            }

            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters.");

            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                errors.Add("Password must contain at least one letter and one digit.");
        }
    }
}
