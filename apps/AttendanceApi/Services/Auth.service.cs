using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AttendanceApi.Common;
using AttendanceApi.Domain.Entities;
using AttendanceApi.DTOs.Auth;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AttendanceApi.Services;

public class AuthService : IAuthService
{
    private readonly AttendanceDbContext _context;
    private readonly JwtOptions _jwtOptions;

    public AuthService(AttendanceDbContext context, IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");
        }

        var accessTokenExpires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);
        var accessToken = GenerateJwtToken(user, accessTokenExpires);
        var refreshToken = GenerateRefreshToken(ipAddress);

        user.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = accessTokenExpires,
            User = MapUserProfile(user)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string token, string ipAddress)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(r => r.User)
                .ThenInclude(u => u!.Employee)
            .FirstOrDefaultAsync(r => r.Token == token);

        if (refreshToken == null || !refreshToken.IsActive || refreshToken.User == null || !refreshToken.User.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token không hợp lệ hoặc đã hết hạn.");
        }

        // Đánh dấu token cũ là revoked
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;

        // Sinh token mới
        var newRefreshToken = GenerateRefreshToken(ipAddress);
        refreshToken.User.RefreshTokens.Add(newRefreshToken);

        var accessTokenExpires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);
        var newAccessToken = GenerateJwtToken(refreshToken.User, accessTokenExpires);

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            AccessTokenExpiresAt = accessTokenExpires,
            User = MapUserProfile(refreshToken.User)
        };
    }

    public async Task<bool> RevokeTokenAsync(string token, string ipAddress)
    {
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (refreshToken == null || !refreshToken.IsActive)
        {
            return false;
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException("Người dùng không tồn tại.");
        }

        return MapUserProfile(user);
    }

    private string GenerateJwtToken(User user, DateTime expires)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString().ToUpper())
        };

        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(string ipAddress)
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static UserProfileDto MapUserProfile(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString().ToUpper(),
            EmployeeId = user.EmployeeId,
            FullName = user.Employee?.FullName
        };
    }
}