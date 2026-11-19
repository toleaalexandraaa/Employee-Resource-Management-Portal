using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace company_resource_manager.Server.Data;

/// <summary>
/// Enables `dotnet ef migrations add` to work from the CLI without a running application.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=DESKTOP-1TFMV12;Database=CompanyResourceManager;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new AppDbContext(optionsBuilder.Options, new HttpContextAccessor());
    }
}
