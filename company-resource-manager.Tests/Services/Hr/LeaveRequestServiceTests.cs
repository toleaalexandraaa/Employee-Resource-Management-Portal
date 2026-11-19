using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Models.Shared;
using company_resource_manager.Server.Services.Hr;
using company_resource_manager.Server.Services.Interfaces;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace company_resource_manager.Tests.Services.Hr;

public class LeaveRequestServiceTests
{
    private readonly Mock<INotificationService> _notifications;
    private readonly Mock<UserManager<ApplicationUser>> _userManager;

    public LeaveRequestServiceTests()
    {
        _notifications = new Mock<INotificationService>();
        _userManager = MockIdentityFactory.CreateUserManager();
    }

    private LeaveRequestService CreateService(AppDbContext db) =>
        new(db, _notifications.Object, _userManager.Object);

    private static Employee CreateEmployee(string first = "John", string last = "Doe") =>
        new() { Id = Guid.NewGuid(), FirstName = first, LastName = last };

    private static LeaveRequest BuildLeaveRequest(Employee employee, LeaveStatus status = LeaveStatus.Pending) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            Employee = employee,
            LeaveType = LeaveType.Annual,
            StartDate = new DateOnly(2026, 3, 10),
            EndDate = new DateOnly(2026, 3, 14),
            WorkingDaysCount = 5,
            Reason = "Vacation",
            Status = status,
        };

    // ── SubmitAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitAsync_CreatesLeaveRequest_WithPendingStatus()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        _userManager.Setup(m => m.GetUsersInRoleAsync("HrManager")).ReturnsAsync([]);

        var request = new CreateLeaveRequest(LeaveType.Annual, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 12), 3, "Vacation");
        var result = await CreateService(db).SubmitAsync(employee.Id, request);

        result.Status.Should().Be(LeaveStatus.Pending);
        result.EmployeeId.Should().Be(employee.Id);
        result.LeaveType.Should().Be(LeaveType.Annual);
        db.LeaveRequests.Should().HaveCount(1);
    }

    [Fact]
    public async Task SubmitAsync_NotifiesEveryHrManager()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var mgr1 = new ApplicationUser { Id = Guid.NewGuid() };
        var mgr2 = new ApplicationUser { Id = Guid.NewGuid() };
        _userManager.Setup(m => m.GetUsersInRoleAsync("HrManager")).ReturnsAsync([mgr1, mgr2]);

        var request = new CreateLeaveRequest(LeaveType.Annual, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 12), 3, null);
        await CreateService(db).SubmitAsync(employee.Id, request);

        _notifications.Verify(n => n.CreateAsync(mgr1.Id, It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _notifications.Verify(n => n.CreateAsync(mgr2.Id, It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_DoesNotNotify_WhenNoHrManagersExist()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        _userManager.Setup(m => m.GetUsersInRoleAsync("HrManager")).ReturnsAsync([]);

        var request = new CreateLeaveRequest(LeaveType.Medical, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 2), 2, null);
        await CreateService(db).SubmitAsync(employee.Id, request);

        _notifications.Verify(n => n.CreateAsync(It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── ApproveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAsync_SetsStatusToApproved_AndUpdatesExistingBalance()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee);      // 5 working days
        var balance = new LeaveBalance
        {
            Id = Guid.NewGuid(), EmployeeId = employee.Id,
            Year = 2026, LeaveType = LeaveType.Annual, TotalDays = 21, UsedDays = 3,
        };
        db.LeaveRequests.Add(leave);
        db.LeaveBalances.Add(balance);
        await db.SaveChangesAsync();
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        var result = await CreateService(db).ApproveAsync(leave.Id, Guid.NewGuid());

        result.Should().NotBeNull();
        result!.Status.Should().Be(LeaveStatus.Approved);
        db.LeaveBalances.Find(balance.Id)!.UsedDays.Should().Be(8);  // 3 + 5
    }

    [Fact]
    public async Task ApproveAsync_CreatesNewBalance_WhenNoneExistsForThatYearAndType()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee);
        leave.LeaveType = LeaveType.Medical;
        leave.WorkingDaysCount = 3;
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        await CreateService(db).ApproveAsync(leave.Id, Guid.NewGuid());

        var newBalance = db.LeaveBalances.Single();
        newBalance.TotalDays.Should().Be(21);
        newBalance.UsedDays.Should().Be(3);
        newBalance.LeaveType.Should().Be(LeaveType.Medical);
    }

    [Fact]
    public async Task ApproveAsync_SetsReviewerAndTimestamp()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee);
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        var reviewerId = Guid.NewGuid();
        var before = DateTime.UtcNow;
        await CreateService(db).ApproveAsync(leave.Id, reviewerId);
        var after = DateTime.UtcNow;

        var stored = db.LeaveRequests.Find(leave.Id)!;
        stored.ReviewedBy.Should().Be(reviewerId);
        stored.ReviewedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task ApproveAsync_NotifiesEmployee_WhenUserFound()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee);
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();

        var employeeUser = new ApplicationUser { Id = Guid.NewGuid(), EmployeeId = employee.Id };
        _userManager.Setup(m => m.Users)
            .Returns(new TestAsyncEnumerable<ApplicationUser>([employeeUser]));

        await CreateService(db).ApproveAsync(leave.Id, Guid.NewGuid());

        _notifications.Verify(n => n.CreateAsync(
            employeeUser.Id,
            NotificationType.LeaveRequestApproved,
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_ReturnsNull_WhenLeaveIsAlreadyApproved()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee, LeaveStatus.Approved);
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();

        var result = await CreateService(db).ApproveAsync(leave.Id, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ApproveAsync_ReturnsNull_WhenLeaveIsRejected()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee, LeaveStatus.Rejected);
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();

        var result = await CreateService(db).ApproveAsync(leave.Id, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ApproveAsync_ReturnsNull_WhenLeaveNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).ApproveAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── RejectAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectAsync_SetsStatusToRejected()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee);
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        var result = await CreateService(db).RejectAsync(leave.Id, Guid.NewGuid(), "No cover available");

        result.Should().NotBeNull();
        result!.Status.Should().Be(LeaveStatus.Rejected);
    }

    [Fact]
    public async Task RejectAsync_OverwritesReason_WhenReasonProvided()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee);
        leave.Reason = "Original reason";
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        await CreateService(db).RejectAsync(leave.Id, Guid.NewGuid(), "Manager reason");

        db.LeaveRequests.Find(leave.Id)!.Reason.Should().Be("Manager reason");
    }

    [Fact]
    public async Task RejectAsync_NotifiesEmployee_WhenUserFound()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee);
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();

        var employeeUser = new ApplicationUser { Id = Guid.NewGuid(), EmployeeId = employee.Id };
        _userManager.Setup(m => m.Users)
            .Returns(new TestAsyncEnumerable<ApplicationUser>([employeeUser]));

        await CreateService(db).RejectAsync(leave.Id, Guid.NewGuid(), null);

        _notifications.Verify(n => n.CreateAsync(
            employeeUser.Id,
            NotificationType.LeaveRequestRejected,
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_ReturnsNull_ForNonPendingLeave()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee, LeaveStatus.Approved);
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RejectAsync(leave.Id, Guid.NewGuid(), null);

        result.Should().BeNull();
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_ReturnsTrue_WhenOwnPendingLeaveExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee);
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();

        var result = await CreateService(db).CancelAsync(leave.Id, employee.Id);

        result.Should().BeTrue();
        // Soft-delete converts DELETE to UPDATE with IsDeleted = true,
        // so count with ignored filter should be 1; with filter it should be 0
        db.LeaveRequests.IgnoreQueryFilters().Should().HaveCount(1);
        db.LeaveRequests.IgnoreQueryFilters().Single().IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task CancelAsync_ReturnsFalse_WhenLeaveIsAlreadyApproved()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var leave = BuildLeaveRequest(employee, LeaveStatus.Approved);
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();

        var result = await CreateService(db).CancelAsync(leave.Id, employee.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CancelAsync_ReturnsFalse_WhenCancellingAnotherEmployeesLeave()
    {
        using var db = InMemoryDbContextFactory.Create();
        var owner = CreateEmployee("Owner", "One");
        var other = CreateEmployee("Other", "Two");
        db.Employees.AddRange(owner, other);
        var leave = BuildLeaveRequest(owner);
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();

        var result = await CreateService(db).CancelAsync(leave.Id, other.Id);

        result.Should().BeFalse();
    }

    // ── GetCalendarAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetCalendarAsync_ReturnsOnlyApproved_WithinDateRange()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);

        var approved = new LeaveRequest
        {
            Id = Guid.NewGuid(), EmployeeId = employee.Id, Employee = employee,
            LeaveType = LeaveType.Annual, Status = LeaveStatus.Approved,
            StartDate = new DateOnly(2026, 3, 10), EndDate = new DateOnly(2026, 3, 14),
            WorkingDaysCount = 5, Reason = null,
        };
        var pending = new LeaveRequest
        {
            Id = Guid.NewGuid(), EmployeeId = employee.Id, Employee = employee,
            LeaveType = LeaveType.Annual, Status = LeaveStatus.Pending,
            StartDate = new DateOnly(2026, 3, 15), EndDate = new DateOnly(2026, 3, 17),
            WorkingDaysCount = 3, Reason = null,
        };
        var outOfRange = new LeaveRequest
        {
            Id = Guid.NewGuid(), EmployeeId = employee.Id, Employee = employee,
            LeaveType = LeaveType.Annual, Status = LeaveStatus.Approved,
            StartDate = new DateOnly(2026, 4, 1), EndDate = new DateOnly(2026, 4, 3),
            WorkingDaysCount = 3, Reason = null,
        };
        db.LeaveRequests.AddRange(approved, pending, outOfRange);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetCalendarAsync(
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(approved.Id);
    }

    // ── GetBalancesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetBalancesAsync_ReturnsBalances_ForRequestedEmployeeAndYear()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        var otherEmployee = CreateEmployee("Jane", "Smith");
        db.Employees.Add(otherEmployee);

        db.LeaveBalances.AddRange(
            new LeaveBalance { Id = Guid.NewGuid(), EmployeeId = employee.Id, Year = 2026, LeaveType = LeaveType.Annual, TotalDays = 21, UsedDays = 5 },
            new LeaveBalance { Id = Guid.NewGuid(), EmployeeId = employee.Id, Year = 2026, LeaveType = LeaveType.Medical,   TotalDays = 10, UsedDays = 2 },
            new LeaveBalance { Id = Guid.NewGuid(), EmployeeId = employee.Id, Year = 2025, LeaveType = LeaveType.Annual, TotalDays = 21, UsedDays = 15 }, // wrong year
            new LeaveBalance { Id = Guid.NewGuid(), EmployeeId = otherEmployee.Id, Year = 2026, LeaveType = LeaveType.Annual, TotalDays = 21, UsedDays = 0 } // wrong employee
        );
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetBalancesAsync(employee.Id, 2026);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b =>
        {
            b.EmployeeId.Should().Be(employee.Id);
            b.Year.Should().Be(2026);
        });
    }

    [Fact]
    public async Task GetBalancesAsync_CalculatesRemainingDaysCorrectly()
    {
        using var db = InMemoryDbContextFactory.Create();
        var employee = CreateEmployee();
        db.Employees.Add(employee);
        db.LeaveBalances.Add(new LeaveBalance
        {
            Id = Guid.NewGuid(), EmployeeId = employee.Id,
            Year = 2026, LeaveType = LeaveType.Annual, TotalDays = 21, UsedDays = 8,
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetBalancesAsync(employee.Id, 2026);

        result.Single().RemainingDays.Should().Be(13);  // 21 - 8
    }
}
