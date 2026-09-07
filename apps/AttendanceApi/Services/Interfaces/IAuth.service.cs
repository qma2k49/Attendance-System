using AttendanceApi.DTOs.Auth;

namespace AttendanceApi.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<bool> RevokeTokenAsync(string refreshToken, string ipAddress);
    Task<UserProfileDto> GetCurrentUserProfileAsync(int userId);
}   