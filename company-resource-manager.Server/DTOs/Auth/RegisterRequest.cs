namespace company_resource_manager.Server.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string FullName, string Role = "Employee");
