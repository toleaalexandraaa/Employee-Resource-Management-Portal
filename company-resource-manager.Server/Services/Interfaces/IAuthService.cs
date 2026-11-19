using company_resource_manager.Server.DTOs.Auth;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RefreshAsync(string refreshToken);
    Task<bool> RevokeAsync(Guid userId);
    Task<(bool Success, string[] Errors)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string[] Errors)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}
