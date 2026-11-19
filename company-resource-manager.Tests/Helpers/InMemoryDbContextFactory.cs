using company_resource_manager.Server.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace company_resource_manager.Tests.Helpers;

/// <summary>
/// Creates an isolated AppDbContext backed by an in-memory database.
/// Each call produces a fresh database so tests don't share state.
/// The IHttpContextAccessor is mocked to return null (audit log will use Guid.Empty as actor).
/// </summary>
public static class InMemoryDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        return new AppDbContext(options, httpContextAccessor.Object);
    }
}
