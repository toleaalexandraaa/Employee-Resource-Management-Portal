using Microsoft.AspNetCore.Identity;

namespace company_resource_manager.Server.Services.Seeding;

public class RoleSeederService : IHostedService
{
    private static readonly string[] Roles =
    [
        "Admin", "HrManager", "ProjectManager", "AccountManager", "Employee",
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RoleSeederService> _logger;

    public RoleSeederService(IServiceScopeFactory scopeFactory, ILogger<RoleSeederService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                _logger.LogInformation("Seeded role: {Role}", role);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
