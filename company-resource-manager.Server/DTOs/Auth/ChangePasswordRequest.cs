namespace company_resource_manager.Server.DTOs.Auth;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
