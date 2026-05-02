using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskManagerApi.Dtos;
using TaskManagerApi.Helpers;
using TaskManagerApi.Models;
using TaskManagerApi.Options;

namespace TaskManagerApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthController(AppDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <param name="dto">Registration payload containing email and password.</param>
        /// <returns>Returns <c>200 OK</c> when registration succeeds.</returns>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var emailExists = await _context.Users.AnyAsync(x => x.Email == dto.Email);

            if (emailExists)
                return Conflict("Email already exists");

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = PasswordHelper.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Authenticates a user and returns a JWT access token and refresh token.
        /// </summary>
        /// <param name="dto">Login payload containing email and password.</param>
        /// <returns>A token pair when credentials are valid.</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null)
                return Unauthorized("User not found");

            var hashedPassword = PasswordHelper.HashPassword(dto.Password);

            if (user.PasswordHash != hashedPassword)
                return Unauthorized("Wrong password");

            var tokenPair = CreateTokenPair(user);
            SetRefreshToken(user, tokenPair.RefreshToken);
            await _context.SaveChangesAsync();

            return Ok(tokenPair);
        }

        /// <summary>
        /// Exchanges a valid refresh token for a new access token and refresh token.
        /// </summary>
        /// <param name="dto">Refresh token payload.</param>
        /// <returns>A rotated token pair when the refresh token is valid.</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
        {
            var refreshTokenHash = PasswordHelper.HashValue(dto.RefreshToken);

            var user = await _context.Users.FirstOrDefaultAsync(x => x.RefreshTokenHash == refreshTokenHash);

            if (user == null || user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token");

            var tokenPair = CreateTokenPair(user);
            SetRefreshToken(user, tokenPair.RefreshToken);
            await _context.SaveChangesAsync();

            return Ok(tokenPair);
        }

        /// <summary>
        /// Revokes the currently authenticated user's refresh token.
        /// </summary>
        /// <returns>Returns <c>200 OK</c> when logout succeeds.</returns>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized();

            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            await _context.SaveChangesAsync();

            return Ok();
        }

        private LoginResponseDto CreateTokenPair(User user)
        {
            var accessToken = CreateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        private string CreateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void SetRefreshToken(User user, string refreshToken)
        {
            user.RefreshTokenHash = PasswordHelper.HashValue(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);
        }

        private static string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }
    }
}
