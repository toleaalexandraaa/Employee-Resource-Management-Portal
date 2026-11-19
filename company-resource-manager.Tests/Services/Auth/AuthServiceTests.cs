using company_resource_manager.Server.DTOs.Auth;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Services.Auth;
using company_resource_manager.Server.Settings;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;

namespace company_resource_manager.Tests.Services.Auth;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManager;
    private readonly IOptions<JwtSettings> _jwtOptions;

    public AuthServiceTests()
    {
        _userManager = MockIdentityFactory.CreateUserManager();
        _signInManager = MockIdentityFactory.CreateSignInManager(_userManager);

        _jwtOptions = Options.Create(new JwtSettings
        {
            Secret = "super-secret-key-for-testing-purposes-that-is-256-bits-long!!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7,
        });
    }

    private AuthService CreateService(Server.Data.AppDbContext db) =>
        new(_userManager.Object, _signInManager.Object, db, _jwtOptions);

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenUserNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                    .ReturnsAsync((ApplicationUser?)null);

        var result = await CreateService(db).LoginAsync(new LoginRequest("unknown@test.com", "pass"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenUserIsInactive()
    {
        using var db = InMemoryDbContextFactory.Create();
        var user = new ApplicationUser { IsActive = false, Email = "test@test.com", FullName = "Test" };
        _userManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await CreateService(db).LoginAsync(new LoginRequest("test@test.com", "pass"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenPasswordCheckFails()
    {
        using var db = InMemoryDbContextFactory.Create();
        var user = new ApplicationUser { IsActive = true, Email = "test@test.com", FullName = "Test" };
        _userManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        _signInManager.Setup(m => m.CheckPasswordSignInAsync(user, "wrong", true))
                      .ReturnsAsync(SignInResult.Failed);

        var result = await CreateService(db).LoginAsync(new LoginRequest("test@test.com", "wrong"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ReturnsAuthResponse_WhenCredentialsAreValid()
    {
        using var db = InMemoryDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            Email = "admin@test.com",
            UserName = "admin@test.com",
            FullName = "Admin User",
        };
        _userManager.Setup(m => m.FindByEmailAsync("admin@test.com")).ReturnsAsync(user);
        _signInManager.Setup(m => m.CheckPasswordSignInAsync(user, "Password1!", true))
                      .ReturnsAsync(SignInResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

        var result = await CreateService(db).LoginAsync(new LoginRequest("admin@test.com", "Password1!"));

        result.Should().NotBeNull();
        result!.FullName.Should().Be("Admin User");
        result.Role.Should().Be("Admin");
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_DefaultsRoleToEmployee_WhenUserHasNoRoles()
    {
        using var db = InMemoryDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), IsActive = true,
            Email = "user@test.com", UserName = "user@test.com", FullName = "Plain User",
        };
        _userManager.Setup(m => m.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        _signInManager.Setup(m => m.CheckPasswordSignInAsync(user, "pass", true))
                      .ReturnsAsync(SignInResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);  // no roles

        var result = await CreateService(db).LoginAsync(new LoginRequest("user@test.com", "pass"));

        result!.Role.Should().Be("Employee");
    }

    [Fact]
    public async Task LoginAsync_PersistsRefreshToken_InDatabase()
    {
        using var db = InMemoryDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), IsActive = true,
            Email = "user@test.com", UserName = "user@test.com", FullName = "User",
        };
        _userManager.Setup(m => m.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        _signInManager.Setup(m => m.CheckPasswordSignInAsync(user, "pass", true))
                      .ReturnsAsync(SignInResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Employee"]);

        var result = await CreateService(db).LoginAsync(new LoginRequest("user@test.com", "pass"));

        db.RefreshTokens.Should().HaveCount(1);
        db.RefreshTokens.First().Token.Should().Be(result!.RefreshToken);
        db.RefreshTokens.First().UserId.Should().Be(user.Id);
    }

    // ── RefreshAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_ReturnsNull_WhenTokenDoesNotExist()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).RefreshAsync("nonexistent-token");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNull_WhenTokenIsRevoked()
    {
        using var db = InMemoryDbContextFactory.Create();
        var userId = Guid.NewGuid();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = userId, Token = "revoked",
            IsRevoked = true, ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).RefreshAsync("revoked");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNull_WhenTokenIsExpired()
    {
        using var db = InMemoryDbContextFactory.Create();
        var userId = Guid.NewGuid();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = userId, Token = "expired",
            IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).RefreshAsync("expired");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_RevokesOldToken_AndIssuesNewPair()
    {
        using var db = InMemoryDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), IsActive = true,
            Email = "user@test.com", UserName = "user@test.com", FullName = "User",
        };
        db.Users.Add(user);
        var oldTokenId = Guid.NewGuid();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = oldTokenId, UserId = user.Id, Token = "valid-old-token",
            IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        _userManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(["Employee"]);

        var result = await CreateService(db).RefreshAsync("valid-old-token");

        result.Should().NotBeNull();
        // Old token must be revoked
        db.RefreshTokens.Find(oldTokenId)!.IsRevoked.Should().BeTrue();
        // A new refresh token must have been persisted
        db.RefreshTokens.Count(t => !t.IsRevoked).Should().Be(1);
        db.RefreshTokens.Single(t => !t.IsRevoked).Token.Should().Be(result!.RefreshToken);
    }

    // ── RevokeAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAsync_RevokesAllActiveTokens_ForUser()
    {
        using var db = InMemoryDbContextFactory.Create();
        var userId = Guid.NewGuid();
        db.RefreshTokens.AddRange(
            new RefreshToken { Id = Guid.NewGuid(), UserId = userId, Token = "t1", IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow },
            new RefreshToken { Id = Guid.NewGuid(), UserId = userId, Token = "t2", IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow },
            new RefreshToken { Id = Guid.NewGuid(), UserId = userId, Token = "t3", IsRevoked = true,  ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var result = await CreateService(db).RevokeAsync(userId);

        result.Should().BeTrue();
        db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeAsync_DoesNotAffect_OtherUsersTokens()
    {
        using var db = InMemoryDbContextFactory.Create();
        var targetUser = Guid.NewGuid();
        var otherUser  = Guid.NewGuid();
        db.RefreshTokens.AddRange(
            new RefreshToken { Id = Guid.NewGuid(), UserId = targetUser, Token = "target", IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow },
            new RefreshToken { Id = Guid.NewGuid(), UserId = otherUser,  Token = "other",  IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        await CreateService(db).RevokeAsync(targetUser);

        db.RefreshTokens.Single(t => t.UserId == otherUser).IsRevoked.Should().BeFalse();
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ReturnsSuccess_WhenRequestIsValid()
    {
        using var db = InMemoryDbContextFactory.Create();
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
                    .ReturnsAsync(IdentityResult.Success);

        var (success, errors) = await CreateService(db).RegisterAsync(
            new RegisterRequest("new@test.com", "Password1!", "New User", "Admin"));

        success.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_ReturnsErrors_WhenUserCreationFails()
    {
        using var db = InMemoryDbContextFactory.Create();
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        var (success, errors) = await CreateService(db).RegisterAsync(
            new RegisterRequest("new@test.com", "weak", "New User", "Admin"));

        success.Should().BeFalse();
        errors.Should().Contain("Password too weak");
    }

    [Fact]
    public async Task RegisterAsync_ReturnsErrors_WhenRoleAssignmentFails()
    {
        using var db = InMemoryDbContextFactory.Create();
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role not found" }));

        var (success, errors) = await CreateService(db).RegisterAsync(
            new RegisterRequest("new@test.com", "Password1!", "New User", "NonExistentRole"));

        success.Should().BeFalse();
        errors.Should().Contain("Role not found");
    }
}
