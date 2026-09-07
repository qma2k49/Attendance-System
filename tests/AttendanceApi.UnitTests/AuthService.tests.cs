using AttendanceApi.Common;
using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Auth;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class AuthServiceTests
{
    private AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AttendanceDbContext(options);
    }

    private IOptions<JwtOptions> CreateMockJwtOptions()
    {
        return Options.Create(new JwtOptions
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SecretKey = "SuperSecretKeyForTestingSprint6AuthServiceTokens2026!",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        });
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAccessAndRefreshToken()
    {
        using var context = CreateInMemoryDbContext();
        var rawPassword = "SecurePassword123@";
        var user = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            Email = "admin@example.com",
            Role = UserRole.Admin,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, CreateMockJwtOptions());
        var request = new LoginRequestDto { Username = "admin", Password = rawPassword };

        var result = await authService.LoginAsync(request, "127.0.0.1");

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Equal("ADMIN", result.User.Role);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowUnauthorizedAccessException()
    {
        using var context = CreateInMemoryDbContext();
        var user = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            Email = "admin@example.com",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, CreateMockJwtOptions());
        var request = new LoginRequestDto { Username = "admin", Password = "WrongPassword" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredOrRevokedToken_ShouldThrowUnauthorizedAccessException()
    {
        using var context = CreateInMemoryDbContext();
        var user = new User
        {
            Username = "user1",
            PasswordHash = "hash",
            Email = "user1@example.com",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var expiredToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "expired_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // Đã hết hạn
            IsRevoked = false
        };
        context.RefreshTokens.Add(expiredToken);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, CreateMockJwtOptions());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.RefreshTokenAsync("expired_refresh_token", "127.0.0.1"));
    }
}