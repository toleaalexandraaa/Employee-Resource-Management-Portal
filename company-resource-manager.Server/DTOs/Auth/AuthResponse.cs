namespace company_resource_manager.Server.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId,
    string FullName,
    string Role
);
