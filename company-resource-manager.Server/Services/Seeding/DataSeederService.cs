using company_resource_manager.Server.Data;
using company_resource_manager.Server.Models.Crm;
using TaskStatus = company_resource_manager.Server.Models.Proj.TaskStatus;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Models.Proj;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Seeding;

/// <summary>
/// Seeds the database with rich demo data for charts and ML features.
/// - 15 employees across 3 performance tiers (High / Standard / Low) for employee clustering
/// - 10 clients across 3 revenue/payment tiers (Key Account / Growth / At Risk) for client segmentation
/// - 25 completed projects (20 on-track, 5 at-risk) for risk model training
/// - 5 active projects with diverse risk profiles for scoring
/// </summary>
public class DataSeederService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataSeederService> _logger;

    public DataSeederService(IServiceScopeFactory scopeFactory, ILogger<DataSeederService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();

        // ── Test accounts (always seeded, safe to re-run) ─────────────────────────
        await CreateUser(userManager, passwordHasher, "admin@admin.com", "admin", "System Administrator", "Admin");
        await CreateUser(userManager, passwordHasher, "employee@employee.com", "employee", "Employee", "Employee");
        await CreateUser(userManager, passwordHasher, "hrmanager@test.com", "hrmanager", "HR Manager", "HrManager");
        await CreateUser(userManager, passwordHasher, "projectmanager@test.com", "projectmanager", "Project Manager", "ProjectManager");
        await CreateUser(userManager, passwordHasher, "accountmanager@test.com", "accountmanager", "Account Manager", "AccountManager");

        if (await userManager.FindByEmailAsync("admin@admin.com") is not null
            && await db.Employees.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already seeded – skipping DataSeederService.");
            return;
        }

        _logger.LogInformation("Seeding database with rich demo data…");

        // ── 1. Users ──────────────────────────────────────────────────────────────

        var adminUser = await CreateUser(userManager, passwordHasher, "admin@admin.com", "admin", "System Administrator", "Admin");
        var sarahUser = await CreateUser(userManager, passwordHasher, "sarah.johnson@company.com", "Password1", "Sarah Johnson", "HrManager");
        var michaelUser = await CreateUser(userManager, passwordHasher, "michael.chen@company.com", "Password1", "Michael Chen", "ProjectManager");
        var emilyUser = await CreateUser(userManager, passwordHasher, "emily.rodriguez@company.com", "Password1", "Emily Rodriguez", "AccountManager");
        var jamesUser = await CreateUser(userManager, passwordHasher, "james.wilson@company.com", "Password1", "James Wilson", "Employee");
        var lisaUser = await CreateUser(userManager, passwordHasher, "lisa.anderson@company.com", "Password1", "Lisa Anderson", "Employee");
        var alexUser = await CreateUser(userManager, passwordHasher, "alex.popescu@company.com", "Password1", "Alex Popescu", "Employee");
        var mariaUser = await CreateUser(userManager, passwordHasher, "maria.constantin@company.com", "Password1", "Maria Constantin", "Employee");
        var andreiUser = await CreateUser(userManager, passwordHasher, "andrei.dumitrescu@company.com", "Password1", "Andrei Dumitrescu", "Employee");
        var elenaUser = await CreateUser(userManager, passwordHasher, "elena.popa@company.com", "Password1", "Elena Popa", "Employee");
        var raduUser = await CreateUser(userManager, passwordHasher, "radu.ionescu@company.com", "Password1", "Radu Ionescu", "Employee");
        var catalinUser = await CreateUser(userManager, passwordHasher, "catalin.gheorghe@company.com", "Password1", "Catalin Gheorghe", "Employee");
        var victorUser = await CreateUser(userManager, passwordHasher, "victor.stanescu@company.com", "Password1", "Victor Stanescu", "Employee");
        var ioanaUser = await CreateUser(userManager, passwordHasher, "ioana.stoica@company.com", "Password1", "Ioana Stoica", "Employee");
        var bogdanUser = await CreateUser(userManager, passwordHasher, "bogdan.munteanu@company.com", "Password1", "Bogdan Munteanu", "Employee");
        var carmenUser = await CreateUser(userManager, passwordHasher, "carmen.rusu@company.com", "Password1", "Carmen Rusu", "Employee");

        // ── 2. Employees ──────────────────────────────────────────────────────────
        // Cluster intent: High Performer (Alex, Maria, Andrei) | Standard (Elena–James) | Low Util (Ioana–Carmen + Sarah + Emily)

        var empSarah = Emp("Sarah", "Johnson", "sarah.johnson@company.com", "Human Resources", "HR Manager", new DateOnly(1985, 3, 12), new DateOnly(2019, 6, 1), 5.0f);
        var empMichael = Emp("Michael", "Chen", "michael.chen@company.com", "Engineering", "Project Manager", new DateOnly(1988, 7, 22), new DateOnly(2020, 1, 15), 4.0f);
        var empEmily = Emp("Emily", "Rodriguez", "emily.rodriguez@company.com", "Sales", "Account Manager", new DateOnly(1990, 11, 5), new DateOnly(2021, 3, 1), 3.0f);
        var empJames = Emp("James", "Wilson", "james.wilson@company.com", "Engineering", "Senior Developer", new DateOnly(1992, 4, 18), new DateOnly(2021, 9, 1), 2.5f);
        var empLisa = Emp("Lisa", "Anderson", "lisa.anderson@company.com", "Engineering", "Frontend Developer", new DateOnly(1995, 8, 30), new DateOnly(2022, 5, 16), 1.5f);
        // High Performers
        var empAlex = Emp("Alex", "Popescu", "alex.popescu@company.com", "Engineering", "Lead Developer", new DateOnly(1987, 5, 20), new DateOnly(2018, 2, 1), 8.0f);
        var empMaria = Emp("Maria", "Constantin", "maria.constantin@company.com", "Engineering", "Full-Stack Dev", new DateOnly(1989, 11, 15), new DateOnly(2019, 9, 1), 6.0f);
        var empAndrei = Emp("Andrei", "Dumitrescu", "andrei.dumitrescu@company.com", "Engineering", "Backend Developer", new DateOnly(1991, 3, 8), new DateOnly(2020, 6, 1), 5.0f);
        // Standard
        var empElena = Emp("Elena", "Popa", "elena.popa@company.com", "Engineering", "Full-Stack Dev", new DateOnly(1993, 7, 25), new DateOnly(2022, 1, 10), 3.0f);
        var empRadu = Emp("Radu", "Ionescu", "radu.ionescu@company.com", "Engineering", "Backend Developer", new DateOnly(1995, 2, 14), new DateOnly(2023, 3, 1), 2.0f);
        var empCatalin = Emp("Catalin", "Gheorghe", "catalin.gheorghe@company.com", "Quality Assurance", "QA Engineer", new DateOnly(1992, 9, 30), new DateOnly(2022, 7, 1), 3.0f);
        var empVictor = Emp("Victor", "Stanescu", "victor.stanescu@company.com", "Engineering", "Data Analyst", new DateOnly(1988, 12, 5), new DateOnly(2020, 11, 1), 5.0f);
        // Low Utilisation
        var empIoana = Emp("Ioana", "Stoica", "ioana.stoica@company.com", "Engineering", "Junior Developer", new DateOnly(1998, 4, 22), new DateOnly(2024, 9, 1), 1.0f);
        var empBogdan = Emp("Bogdan", "Munteanu", "bogdan.munteanu@company.com", "Engineering", "DevOps Engineer", new DateOnly(1990, 6, 18), new DateOnly(2021, 4, 15), 4.0f);
        var empCarmen = Emp("Carmen", "Rusu", "carmen.rusu@company.com", "Design", "UI Designer", new DateOnly(1994, 1, 28), new DateOnly(2023, 1, 1), 2.0f);

        db.Employees.AddRange(empSarah, empMichael, empEmily, empJames, empLisa,
            empAlex, empMaria, empAndrei, empElena, empRadu,
            empCatalin, empVictor, empIoana, empBogdan, empCarmen);
        await db.SaveChangesAsync(cancellationToken);

        // Link users ↔ employees
        sarahUser.EmployeeId = empSarah.Id; michaelUser.EmployeeId = empMichael.Id;
        emilyUser.EmployeeId = empEmily.Id; jamesUser.EmployeeId = empJames.Id;
        lisaUser.EmployeeId = empLisa.Id; alexUser.EmployeeId = empAlex.Id;
        mariaUser.EmployeeId = empMaria.Id; andreiUser.EmployeeId = empAndrei.Id;
        elenaUser.EmployeeId = empElena.Id; raduUser.EmployeeId = empRadu.Id;
        catalinUser.EmployeeId = empCatalin.Id; victorUser.EmployeeId = empVictor.Id;
        ioanaUser.EmployeeId = empIoana.Id; bogdanUser.EmployeeId = empBogdan.Id;
        carmenUser.EmployeeId = empCarmen.Id;
        await db.SaveChangesAsync(cancellationToken);

        // ── 3. Employment Contracts ───────────────────────────────────────────────

        db.EmploymentContracts.AddRange(
            EmpContract(empSarah.Id, new DateOnly(2019, 6, 1), 8_000m),
            EmpContract(empMichael.Id, new DateOnly(2020, 1, 15), 9_500m),
            EmpContract(empEmily.Id, new DateOnly(2021, 3, 1), 7_500m),
            EmpContract(empJames.Id, new DateOnly(2021, 9, 1), 8_500m),
            EmpContract(empLisa.Id, new DateOnly(2022, 5, 16), 6_500m),
            EmpContract(empAlex.Id, new DateOnly(2018, 2, 1), 11_000m),
            EmpContract(empMaria.Id, new DateOnly(2019, 9, 1), 10_000m),
            EmpContract(empAndrei.Id, new DateOnly(2020, 6, 1), 9_000m),
            EmpContract(empElena.Id, new DateOnly(2022, 1, 10), 7_000m),
            EmpContract(empRadu.Id, new DateOnly(2023, 3, 1), 6_000m),
            EmpContract(empCatalin.Id, new DateOnly(2022, 7, 1), 7_000m),
            EmpContract(empVictor.Id, new DateOnly(2020, 11, 1), 7_500m),
            EmpContract(empIoana.Id, new DateOnly(2024, 9, 1), 5_000m),
            EmpContract(empBogdan.Id, new DateOnly(2021, 4, 15), 7_500m),
            EmpContract(empCarmen.Id, new DateOnly(2023, 1, 1), 6_500m)
        );
        await db.SaveChangesAsync(cancellationToken);

        // ── 4. Leave Requests ─────────────────────────────────────────────────────

        db.LeaveRequests.AddRange(
            // ── Past leaves 2025 — already taken ──────────────────────────────────────
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empAlex.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2025, 7, 14), EndDate = new DateOnly(2025, 7, 25), WorkingDaysCount = 10, Reason = "Summer vacation", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 6, 20, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empMaria.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2025, 8, 4), EndDate = new DateOnly(2025, 8, 15), WorkingDaysCount = 10, Reason = "Summer holiday", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 7, 15, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empAndrei.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2025, 6, 23), EndDate = new DateOnly(2025, 6, 27), WorkingDaysCount = 5, Reason = "Family trip", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empElena.Id, LeaveType = LeaveType.Medical, StartDate = new DateOnly(2025, 9, 15), EndDate = new DateOnly(2025, 9, 17), WorkingDaysCount = 3, Reason = "Medical appointment", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 9, 10, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empJames.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2025, 8, 25), EndDate = new DateOnly(2025, 8, 29), WorkingDaysCount = 5, Reason = "Short holiday", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 8, 5, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empLisa.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2025, 7, 7), EndDate = new DateOnly(2025, 7, 11), WorkingDaysCount = 5, Reason = "Personal time off", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 6, 20, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empVictor.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2025, 12, 22), EndDate = new DateOnly(2025, 12, 31), WorkingDaysCount = 7, Reason = "Year-end holiday", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 11, 15, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empCatalin.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2025, 12, 22), EndDate = new DateOnly(2025, 12, 31), WorkingDaysCount = 7, Reason = "Christmas holidays", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 11, 20, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empRadu.Id, LeaveType = LeaveType.Other, StartDate = new DateOnly(2025, 10, 6), EndDate = new DateOnly(2025, 10, 10), WorkingDaysCount = 5, Reason = "External training program", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 9, 25, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empBogdan.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2025, 9, 12), WorkingDaysCount = 10, Reason = "Summer vacation", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empIoana.Id, LeaveType = LeaveType.Medical, StartDate = new DateOnly(2025, 11, 10), EndDate = new DateOnly(2025, 11, 12), WorkingDaysCount = 3, Reason = "Flu recovery", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 11, 9, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empCarmen.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2025, 8, 18), EndDate = new DateOnly(2025, 8, 22), WorkingDaysCount = 5, Reason = "Holiday", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc) },
            // ── Rejected leave (demonstrates full workflow) ────────────────────────────
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empJames.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2025, 12, 8), EndDate = new DateOnly(2025, 12, 12), WorkingDaysCount = 5, Reason = "Extra holiday (sprint crunch)", Status = LeaveStatus.Rejected, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2025, 11, 28, 0, 0, 0, DateTimeKind.Utc) },
            // ── Early 2026 — past ──────────────────────────────────────────────────────
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empMaria.Id, LeaveType = LeaveType.Medical, StartDate = new DateOnly(2026, 1, 20), EndDate = new DateOnly(2026, 1, 24), WorkingDaysCount = 5, Reason = "Flu", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2026, 1, 19, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empRadu.Id, LeaveType = LeaveType.Unpaid, StartDate = new DateOnly(2026, 2, 10), EndDate = new DateOnly(2026, 2, 14), WorkingDaysCount = 5, Reason = "Personal matters", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empVictor.Id, LeaveType = LeaveType.Medical, StartDate = new DateOnly(2026, 2, 16), EndDate = new DateOnly(2026, 2, 18), WorkingDaysCount = 3, Reason = "Dental surgery", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empRadu.Id, LeaveType = LeaveType.Unpaid, StartDate = new DateOnly(2026, 3, 16), EndDate = new DateOnly(2026, 3, 20), WorkingDaysCount = 5, Reason = "Personal matters", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = DateTime.UtcNow.AddDays(-3) },
            // ── Upcoming ──────────────────────────────────────────────────────────────
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empLisa.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 4, 28), EndDate = new DateOnly(2026, 5, 2), WorkingDaysCount = 5, Reason = "Family event", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = DateTime.UtcNow.AddDays(-5) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empCatalin.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 5, 18), EndDate = new DateOnly(2026, 5, 29), WorkingDaysCount = 10, Reason = "European trip", Status = LeaveStatus.Pending },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empBogdan.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 5, 5), EndDate = new DateOnly(2026, 5, 9), WorkingDaysCount = 5, Reason = "Long weekend getaway", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = DateTime.UtcNow.AddDays(-2) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empElena.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 12), WorkingDaysCount = 10, Reason = "Vacation", Status = LeaveStatus.Pending },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empCarmen.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 6, 15), EndDate = new DateOnly(2026, 6, 19), WorkingDaysCount = 5, Reason = "Summer trip", Status = LeaveStatus.Pending },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empJames.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 7, 14), EndDate = new DateOnly(2026, 7, 25), WorkingDaysCount = 8, Reason = "Summer vacation", Status = LeaveStatus.Pending },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empAlex.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 8, 3), EndDate = new DateOnly(2026, 8, 14), WorkingDaysCount = 10, Reason = "Annual leave", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = DateTime.UtcNow.AddDays(-10) },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empIoana.Id, LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 7, 21), EndDate = new DateOnly(2026, 7, 25), WorkingDaysCount = 5, Reason = "Short break", Status = LeaveStatus.Pending },
            new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = empAndrei.Id, LeaveType = LeaveType.Parental, StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 11, 28), WorkingDaysCount = 65, Reason = "Parental leave", Status = LeaveStatus.Approved, ReviewedBy = sarahUser.Id, ReviewedAt = DateTime.UtcNow.AddDays(-7) }
        );
        await db.SaveChangesAsync(cancellationToken);

        // ── 5. Payroll Records (last 3 months, all 15 employees) ─────────────────

        var allEmps = new[] { empSarah, empMichael, empEmily, empJames, empLisa, empAlex, empMaria, empAndrei, empElena, empRadu, empCatalin, empVictor, empIoana, empBogdan, empCarmen };
        var grossSalaries = new[] { 8000m, 9500m, 7500m, 8500m, 6500m, 11000m, 10000m, 9000m, 7000m, 6000m, 7000m, 7500m, 5000m, 7500m, 6500m };
        // December performance bonus (idx: 5=Alex, 6=Maria, 7=Andrei earn top-performer bonus; others get smaller recognition)
        var decBonuses = new[] { 0m, 0m, 0m, 1500m, 0m, 5000m, 4000m, 3500m, 1200m, 0m, 800m, 1000m, 0m, 0m, 0m };
        // 8 months of payroll: Aug–Dec 2025 (paid) + Jan–Mar 2026 (paid, Mar is latest)
        var payrollMonths = new[] { (8, 2025), (9, 2025), (10, 2025), (11, 2025), (12, 2025), (1, 2026), (2, 2026), (3, 2026) };
        var payrollList = new List<PayrollRecord>();
        for (int e = 0; e < allEmps.Length; e++)
        {
            foreach (var (month, year) in payrollMonths)
            {
                var gross = grossSalaries[e];
                var bonus = (month == 12 && year == 2025) ? decBonuses[e] : 0m;
                var tax = Math.Round((gross + bonus) * 0.10m, 2);
                var soc = Math.Round(gross * 0.35m, 2);
                payrollList.Add(new PayrollRecord
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = allEmps[e].Id,
                    Month = month,
                    Year = year,
                    GrossSalary = gross,
                    TaxDeductions = tax,
                    SocialContributions = soc,
                    NetSalary = gross + bonus - tax - soc,
                    BonusAmount = bonus,
                    MealTicketCount = 20,
                    MealTicketValue = 20m,
                    GeneratedAt = new DateTime(year, month, 28, 0, 0, 0, DateTimeKind.Utc),
                    Status = PayrollStatus.Paid,
                });
            }
        }
        db.PayrollRecords.AddRange(payrollList);
        await db.SaveChangesAsync(cancellationToken);

        // ── 6. CRM Clients ────────────────────────────────────────────────────────
        // Segment intent: Key Account (Tech, Finance, Innovatech) | Growth (RetailPro, MedSoft, CloudBridge, DataFusion) | At Risk (BioNova, Construct, OilPro)

        var clientTech = MkClient("TechCorp Inc.", "Technology", "USA", empEmily.Id);
        var clientFinance = MkClient("Global Finance Ltd.", "Finance", "UK", empEmily.Id);
        var clientInnovatech = MkClient("Innovatech Systems S.A.", "Technology", "Romania", empEmily.Id);
        var clientRetailPro = MkClient("RetailPro SRL", "Retail", "Romania", empEmily.Id);
        var clientMedSoft = MkClient("MedSoft Solutions SRL", "Healthcare", "Romania", empEmily.Id);
        var clientCloudBridge = MkClient("CloudBridge Technology", "Cloud Services", "Germany", empEmily.Id);
        var clientDataFusion = MkClient("DataFusion Analytics SRL", "Analytics", "Romania", empEmily.Id);
        var clientBioNova = MkClient("BioNova Research SRL", "Biotechnology", "Romania", empEmily.Id);
        var clientConstruct = MkClient("Construct Impex SRL", "Construction", "Romania", empEmily.Id);
        var clientOilPro = MkClient("OilPro Energy SRL", "Energy", "Romania", empEmily.Id);

        db.Clients.AddRange(clientTech, clientFinance, clientInnovatech, clientRetailPro, clientMedSoft,
            clientCloudBridge, clientDataFusion, clientBioNova, clientConstruct, clientOilPro);
        await db.SaveChangesAsync(cancellationToken);

        db.ClientContacts.AddRange(
            CC(clientTech.Id, "John Smith", "john.smith@techcorp.com", "+1 415 555 0100", "CTO"),
            CC(clientFinance.Id, "Margaret Taylor", "m.taylor@globalfinance.com", "+44 20 7946 0200", "CEO"),
            CC(clientInnovatech.Id, "Mihai Ionescu", "m.ionescu@innovatech.ro", "+40 21 310 1000", "COO"),
            CC(clientRetailPro.Id, "Dana Cristea", "d.cristea@retailpro.ro", "+40 21 210 2000", "Procurement Manager"),
            CC(clientMedSoft.Id, "Dr. Alina Negru", "a.negru@medsoft.ro", "+40 21 320 3000", "CTO"),
            CC(clientCloudBridge.Id, "Hans Mueller", "h.mueller@cloudbridge.de", "+49 30 1234 5678", "VP Engineering"),
            CC(clientDataFusion.Id, "Oana Vasile", "o.vasile@datafusion.ro", "+40 21 430 4000", "Head of Analytics"),
            CC(clientBioNova.Id, "Dr. Petru Blaga", "p.blaga@bionova.ro", "+40 21 540 5000", "R&D Director"),
            CC(clientConstruct.Id, "Ion Marinescu", "i.marinescu@construct.ro", "+40 21 650 6000", "Project Director"),
            CC(clientOilPro.Id, "Florin Balan", "f.balan@oilpro.ro", "+40 21 760 7000", "CFO")
        );
        await db.SaveChangesAsync(cancellationToken);

        // ── 7. CRM Contracts ──────────────────────────────────────────────────────

        db.CrmContracts.AddRange(
            CrmCon("CTR-2023-001", clientTech.Id, "Retainer Agreement 2023", 180_000m, new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31)),
            CrmCon("CTR-2024-001", clientTech.Id, "Development Services 2024", 200_000m, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
            CrmCon("CTR-2025-001", clientTech.Id, "Annual Support Contract 2025", 100_000m, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
            CrmCon("CTR-2023-002", clientFinance.Id, "Financial Systems Development", 170_000m, new DateOnly(2023, 3, 1), new DateOnly(2025, 6, 30)),
            CrmCon("CTR-2023-003", clientInnovatech.Id, "Enterprise Software Suite", 220_000m, new DateOnly(2023, 2, 1), new DateOnly(2026, 6, 30)),
            CrmCon("CTR-2023-004", clientRetailPro.Id, "Digital Transformation", 130_000m, new DateOnly(2023, 5, 1), new DateOnly(2026, 4, 30)),
            CrmCon("CTR-2024-002", clientMedSoft.Id, "Healthcare Platform Development", 200_000m, new DateOnly(2024, 1, 1), new DateOnly(2026, 12, 31)),
            CrmCon("CTR-2024-003", clientCloudBridge.Id, "Cloud Migration Services", 135_000m, new DateOnly(2024, 6, 1), new DateOnly(2026, 5, 31)),
            CrmCon("CTR-2024-004", clientDataFusion.Id, "Analytics Platform", 75_000m, new DateOnly(2024, 3, 1), new DateOnly(2026, 2, 28)),
            CrmCon("CTR-2024-005", clientBioNova.Id, "Research Data System", 85_000m, new DateOnly(2024, 8, 1), new DateOnly(2025, 12, 31))
        );
        await db.SaveChangesAsync(cancellationToken);

        // ── 8. Leads (7 converted → clients, 5 active pipeline) ──────────────────

        db.Leads.AddRange(
            // Converted leads — boost leadCount feature for key-account clients
            MkLead("TechCorp Inc.", "Mark Davis", "m.davis@techcorp.com", LeadSource.Referral, LeadStage.Won, 120_000m, emilyUser.Id, clientTech.Id),
            MkLead("TechCorp Inc.", "Sarah Lee", "s.lee@techcorp.com", LeadSource.LinkedIn, LeadStage.Won, 80_000m, emilyUser.Id, clientTech.Id),
            MkLead("Global Finance Ltd.", "Peter Brown", "p.brown@globalfinance.com", LeadSource.Event, LeadStage.Won, 95_000m, emilyUser.Id, clientFinance.Id),
            MkLead("Innovatech Systems", "Radu Petrescu", "r.petrescu@innovatech.ro", LeadSource.ColdOutreach, LeadStage.Won, 150_000m, emilyUser.Id, clientInnovatech.Id),
            MkLead("Innovatech Systems", "Ana Coman", "a.coman@innovatech.ro", LeadSource.LinkedIn, LeadStage.Won, 80_000m, emilyUser.Id, clientInnovatech.Id),
            MkLead("RetailPro SRL", "Diana Luca", "d.luca@retailpro.ro", LeadSource.Website, LeadStage.Won, 55_000m, emilyUser.Id, clientRetailPro.Id),
            MkLead("MedSoft Solutions", "Dr. Ene Bogdan", "e.bogdan@medsoft.ro", LeadSource.Referral, LeadStage.Won, 80_000m, emilyUser.Id, clientMedSoft.Id),
            // Active pipeline
            MkLead("StartupXYZ SRL", "Alex Turner", "alex@startupxyz.io", LeadSource.LinkedIn, LeadStage.New, 15_000m, emilyUser.Id, null),
            MkLead("Enterprise Solutions SRL", "Diana Popescu", "d.popescu@enterprise.ro", LeadSource.Referral, LeadStage.Contacted, 80_000m, emilyUser.Id, null),
            MkLead("HealthTech Innovations", "Dr. Ioan Marin", "i.marin@healthtech.ro", LeadSource.Website, LeadStage.ProposalSent, 25_000m, emilyUser.Id, null),
            MkLead("GreenEnergy Corp", "Liviu Stoica", "l.stoica@greenenergy.ro", LeadSource.ColdOutreach, LeadStage.Qualified, 40_000m, emilyUser.Id, null),
            MkLead("LogiTrans SRL", "Mihai Toma", "m.toma@logitrans.ro", LeadSource.Event, LeadStage.New, 20_000m, emilyUser.Id, null)
        );
        await db.SaveChangesAsync(cancellationToken);

        // ── 9. Completed Projects (25) ────────────────────────────────────────────
        // Label = wasLate (ActDays > EstDays) OR wasOverBudget (ActBudget > EstBudget)
        // On-track: ActDays ≤ EstDays AND ActBudget ≤ EstBudget (20 projects)
        // At-risk:  ActDays > EstDays OR ActBudget > EstBudget   (5 projects)
        //
        // ML formula (deterministic for high R²):
        //   ActDays  ≈ 12*TeamSize + 0.25*EstHours + 4*AvgSeniority + 8*IsFirst + noise
        //   ActBudget≈ 8000*TeamSize + 120*EstHours + 2000*AvgSeniority + 3000*IsFirst + noise

        // Per-project estimated hours: [reqHours, implHours, testHours]
        var cpHours = new (decimal Req, decimal Impl, decimal Test)[]
        {
            (50m, 120m, 30m), // [0]  200h
            (30m,  60m, 30m), // [1]  120h
            (60m, 160m, 60m), // [2]  280h
            (40m, 100m, 40m), // [3]  180h
            (20m,  40m, 20m), // [4]   80h
            (15m,  30m, 15m), // [5]   60h
            (50m, 110m, 40m), // [6]  200h
            (35m,  80m, 25m), // [7]  140h
            (60m, 150m, 50m), // [8]  260h
            (25m,  55m, 20m), // [9]  100h
            (55m, 140m, 45m), // [10] 240h
            (40m,  90m, 30m), // [11] 160h
            (30m,  70m, 20m), // [12] 120h
            (25m,  50m, 25m), // [13] 100h
            (35m,  75m, 30m), // [14] 140h
            (20m,  50m, 20m), // [15]  90h
            (15m,  35m, 10m), // [16]  60h
            (45m, 100m, 35m), // [17] 180h
            (35m,  80m, 25m), // [18] 140h
            (25m,  60m, 15m), // [19] 100h
            (70m, 180m, 70m), // [20] 320h
            (50m, 130m, 40m), // [21] 220h
            (30m,  80m, 30m), // [22] 140h
            (55m, 120m, 45m), // [23] 220h
            (40m, 110m, 30m), // [24] 180h
        };

        var cpData = new (string Name, Guid ClientId, DateOnly Start, int EstDays, int ActDays, decimal EstBudget, decimal ActBudget)[]
        {
            // TechCorp — 6 on-track
            // [0] Team3, 200h, Avg4.33, First0 → D=36+50+17+0=103, C=24k+24k+8.7k+0=56.7k
            ("Legacy System Migration",        clientTech.Id,       new DateOnly(2023,  1, 15), 120, 104,  80_000m,  57_000m),
            // [1] Team2, 120h, Avg3.5, First0 → D=24+30+14+0=68, C=16k+14.4k+7k+0=37.4k
            ("API Gateway Implementation",     clientTech.Id,       new DateOnly(2023,  6,  1),  90,  69,  45_000m,  37_500m),
            // [2] Team4, 280h, Avg5.0, First0 → D=48+70+20+0=138, C=32k+33.6k+10k+0=75.6k
            ("Mobile App v1.0",                clientTech.Id,       new DateOnly(2024,  1, 10), 180, 139, 120_000m,  76_000m),
            // [3] Team3, 180h, Avg4.0, First0 → D=36+45+16+0=97, C=24k+21.6k+8k+0=53.6k
            ("Data Warehouse Build",           clientTech.Id,       new DateOnly(2024,  7,  1), 150,  98,  95_000m,  54_000m),
            // [4] Team2, 80h, Avg5.5, First0 → D=24+20+22+0=66, C=16k+9.6k+11k+0=36.6k
            ("Security Audit & Hardening",     clientTech.Id,       new DateOnly(2025,  1,  5),  80,  67,  45_000m,  37_000m),
            // [5] Team2, 60h, Avg2.5, First0 → D=24+15+10+0=49, C=16k+7.2k+5k+0=28.2k
            ("CI/CD Pipeline Overhaul",        clientTech.Id,       new DateOnly(2025,  4,  1),  60,  50,  35_000m,  28_500m),
            // GlobalFinance — 4 on-track
            // [6] Team3, 200h, Avg4.33, First0 → D=36+50+17+0=103, C=24k+24k+8.7k+0=56.7k
            ("Risk Assessment Dashboard",      clientFinance.Id,    new DateOnly(2023,  3,  1), 120, 104,  80_000m,  57_000m),
            // [7] Team3, 140h, Avg4.0, First0 → D=36+35+16+0=87, C=24k+16.8k+8k+0=48.8k
            ("Compliance Reporting System",    clientFinance.Id,    new DateOnly(2023,  9,  1), 100,  88,  60_000m,  49_000m),
            // [8] Team4, 260h, Avg5.25, First0 → D=48+65+21+0=134, C=32k+31.2k+10.5k+0=73.7k
            ("Portfolio Manager App",          clientFinance.Id,    new DateOnly(2024,  3,  1), 180, 135, 130_000m,  74_000m),
            // [9] Team2, 100h, Avg4.0, First0 → D=24+25+16+0=65, C=16k+12k+8k+0=36k
            ("API Integration Hub",            clientFinance.Id,    new DateOnly(2025,  1, 15),  80,  66,  50_000m,  36_000m),
            // Innovatech — 4 on-track
            // [10] Team4, 240h, Avg5.25, First0 → D=48+60+21+0=129, C=32k+28.8k+10.5k+0=71.3k
            ("Enterprise CRM Platform",        clientInnovatech.Id, new DateOnly(2023,  2,  1), 150, 130, 100_000m,  71_500m),
            // [11] Team3, 160h, Avg3.67, First0 → D=36+40+15+0=91, C=24k+19.2k+7.3k+0=50.5k
            ("Business Intelligence Platform", clientInnovatech.Id, new DateOnly(2024,  1, 15), 120,  92,  75_000m,  51_000m),
            // [12] Team3, 120h, Avg3.33, First0 → D=36+30+13+0=79, C=24k+14.4k+6.7k+0=45.1k
            ("HR Self-Service Portal",         clientInnovatech.Id, new DateOnly(2024,  8,  1),  90,  80,  55_000m,  45_500m),
            // [13] Team2, 100h, Avg4.0, First0 → D=24+25+16+0=65, C=16k+12k+8k+0=36k
            ("Supply Chain Dashboard",         clientInnovatech.Id, new DateOnly(2025,  2,  1),  80,  66,  45_000m,  36_000m),
            // RetailPro — 3 on-track
            // [14] Team3, 140h, Avg3.33, First0 → D=36+35+13+0=84, C=24k+16.8k+6.7k+0=47.5k
            ("E-Commerce Redesign",            clientRetailPro.Id,  new DateOnly(2023,  5,  1),  90,  85,  55_000m,  47_500m),
            // [15] Team2, 90h, Avg3.0, First0 → D=24+23+12+0=59, C=16k+10.8k+6k+0=32.8k
            ("Inventory Management System",    clientRetailPro.Id,  new DateOnly(2024,  4,  1),  75,  59,  45_000m,  33_000m),
            // [16] Team2, 60h, Avg2.0, First0 → D=24+15+8+0=47, C=16k+7.2k+4k+0=27.2k
            ("Customer Loyalty App",           clientRetailPro.Id,  new DateOnly(2025,  1, 20),  60,  47,  35_000m,  27_500m),
            // MedSoft — 1 on-track
            // [17] Team3, 180h, Avg4.0, First0 → D=36+45+16+0=97, C=24k+21.6k+8k+0=53.6k
            ("Appointment Management System",  clientMedSoft.Id,    new DateOnly(2024,  3,  1), 120,  98,  70_000m,  54_000m),
            // CloudBridge — 1 on-track  (first project with this client)
            // [18] Team3, 140h, Avg3.5, First1 → D=36+35+14+8=93, C=24k+16.8k+7k+3k=50.8k
            ("SaaS Platform Upgrade",          clientCloudBridge.Id,new DateOnly(2025,  1, 10),  95,  93,  55_000m,  51_000m),
            // DataFusion — 1 on-track  (first project with this client)
            // [19] Team2, 100h, Avg3.5, First1 → D=24+25+14+8=71, C=16k+12k+7k+3k=38k
            ("Analytics Dashboard v2",         clientDataFusion.Id, new DateOnly(2025,  2,  1),  80,  72,  45_000m,  38_000m),
            // ── AT-RISK: 5 projects (still follow formula but with EstDays < ActDays) ──
            // [20] Team5, 320h, Avg4.8, First0 → D=60+80+19+0=159, C=40k+38.4k+9.6k+0=88k
            ("Electronic Health Records",      clientMedSoft.Id,    new DateOnly(2023,  1, 15), 140, 160, 150_000m,  88_000m),
            // [21] Team4, 220h, Avg4.25, First0 → D=48+55+17+0=120, C=32k+26.4k+8.5k+0=66.9k
            ("Microservices Migration",        clientCloudBridge.Id,new DateOnly(2024,  6,  1), 100, 121,  80_000m,  67_000m),
            // [22] Team3, 140h, Avg3.67, First0 → D=36+35+15+0=86, C=24k+16.8k+7.3k+0=48.1k
            ("ML Pipeline Integration",        clientDataFusion.Id, new DateOnly(2024,  3,  1),  70,  87,  60_000m,  48_500m),
            // [23] Team4, 220h, Avg3.75, First1 → D=48+55+15+8=126, C=32k+26.4k+7.5k+3k=68.9k
            ("Research Data Platform",         clientBioNova.Id,    new DateOnly(2024,  8,  1), 100, 127,  85_000m,  69_000m),
            // [24] Team4, 180h, Avg3.75, First1 → D=48+45+15+8=116, C=32k+21.6k+7.5k+3k=64.1k
            ("Project Management System",      clientConstruct.Id,  new DateOnly(2024,  1, 15),  95, 117,  65_000m,  64_500m),
        };

        var completedEntities = new List<(Project Proj, ProjectTask ImplTask)>();
        for (int ci = 0; ci < cpData.Length; ci++)
        {
            var cp = cpData[ci];
            var h = cpHours[ci];
            var proj = new Project
            {
                Id = Guid.NewGuid(),
                Name = cp.Name,
                ClientId = cp.ClientId,
                ProjectManagerId = michaelUser.Id,
                Status = ProjectStatus.Completed,
                StartDate = cp.Start,
                EndDate = cp.Start.AddDays(cp.ActDays),
                EstimatedBudget = cp.EstBudget,
                ActualBudget = cp.ActBudget,
                EstimatedDurationDays = cp.EstDays,
            };
            var req = MkTask(proj.Id, "Requirements & Design", TaskPriority.High, TaskStatus.Done, h.Req, michaelUser.Id);
            var impl = MkTask(proj.Id, "Implementation", TaskPriority.High, TaskStatus.Done, h.Impl, michaelUser.Id);
            var test = MkTask(proj.Id, "Testing & Deployment", TaskPriority.Medium, TaskStatus.Done, h.Test, michaelUser.Id);
            db.Tasks.AddRange(req, impl, test);
            completedEntities.Add((proj, impl));
        }
        db.Projects.AddRange(completedEntities.Select(x => x.Proj));

        // ── Members for completed projects (required for ML training features) ──
        // Vary TeamSize (2-5) and AverageSeniorityYears so the regression model
        // can learn meaningful coefficients for those features.
        db.ProjectMembers.AddRange(
            // [0] Legacy System Migration – $80k, 110d → 3 members, avg 4.3yr
            MkPM(completedEntities[0].Proj.Id, empAlex.Id, "Lead Developer", 40m, cpData[0].Start),
            MkPM(completedEntities[0].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[0].Start),
            MkPM(completedEntities[0].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[0].Start),
            // [1] API Gateway – $41k, 82d → 2 members, avg 3.5yr
            MkPM(completedEntities[1].Proj.Id, empAndrei.Id, "Backend Developer", 40m, cpData[1].Start),
            MkPM(completedEntities[1].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[1].Start),
            // [2] Mobile App v1.0 – $112k, 170d → 4 members, avg 5.0yr
            MkPM(completedEntities[2].Proj.Id, empAlex.Id, "Lead Developer", 40m, cpData[2].Start),
            MkPM(completedEntities[2].Proj.Id, empMaria.Id, "Full-Stack Dev", 40m, cpData[2].Start),
            MkPM(completedEntities[2].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[2].Start),
            MkPM(completedEntities[2].Proj.Id, empCatalin.Id, "QA Engineer", 20m, cpData[2].Start),
            // [3] Data Warehouse – $88k, 140d → 3 members, avg 4.0yr
            MkPM(completedEntities[3].Proj.Id, empVictor.Id, "Data Analyst", 40m, cpData[3].Start),
            MkPM(completedEntities[3].Proj.Id, empAndrei.Id, "Backend Developer", 40m, cpData[3].Start),
            MkPM(completedEntities[3].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[3].Start),
            // [4] Security Audit – $31k, 55d → 2 members, avg 5.5yr
            MkPM(completedEntities[4].Proj.Id, empAlex.Id, "Lead Developer", 40m, cpData[4].Start),
            MkPM(completedEntities[4].Proj.Id, empCatalin.Id, "QA Engineer", 40m, cpData[4].Start),
            // [5] CI/CD Pipeline – $23k, 42d → 2 members, avg 2.5yr
            MkPM(completedEntities[5].Proj.Id, empBogdan.Id, "DevOps Engineer", 40m, cpData[5].Start),
            MkPM(completedEntities[5].Proj.Id, empIoana.Id, "Junior Developer", 40m, cpData[5].Start),
            // [6] Risk Assessment Dashboard – $74k, 112d → 3 members, avg 4.3yr
            MkPM(completedEntities[6].Proj.Id, empVictor.Id, "Data Analyst", 40m, cpData[6].Start),
            MkPM(completedEntities[6].Proj.Id, empAndrei.Id, "Backend Developer", 40m, cpData[6].Start),
            MkPM(completedEntities[6].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[6].Start),
            // [7] Compliance Reporting – $56k, 85d → 3 members, avg 4.0yr
            MkPM(completedEntities[7].Proj.Id, empMaria.Id, "Full-Stack Dev", 40m, cpData[7].Start),
            MkPM(completedEntities[7].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[7].Start),
            MkPM(completedEntities[7].Proj.Id, empCatalin.Id, "QA Engineer", 20m, cpData[7].Start),
            // [8] Portfolio Manager App – $122k, 168d → 4 members, avg 5.25yr
            MkPM(completedEntities[8].Proj.Id, empAlex.Id, "Lead Developer", 40m, cpData[8].Start),
            MkPM(completedEntities[8].Proj.Id, empMaria.Id, "Full-Stack Dev", 40m, cpData[8].Start),
            MkPM(completedEntities[8].Proj.Id, empAndrei.Id, "Backend Developer", 40m, cpData[8].Start),
            MkPM(completedEntities[8].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[8].Start),
            // [9] API Integration Hub – $37k, 70d → 2 members, avg 4.0yr
            MkPM(completedEntities[9].Proj.Id, empAndrei.Id, "Backend Developer", 40m, cpData[9].Start),
            MkPM(completedEntities[9].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[9].Start),
            // [10] Enterprise CRM Platform – $92k, 138d → 4 members, avg 5.25yr
            MkPM(completedEntities[10].Proj.Id, empAlex.Id, "Lead Developer", 40m, cpData[10].Start),
            MkPM(completedEntities[10].Proj.Id, empMaria.Id, "Full-Stack Dev", 40m, cpData[10].Start),
            MkPM(completedEntities[10].Proj.Id, empVictor.Id, "Data Analyst", 40m, cpData[10].Start),
            MkPM(completedEntities[10].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[10].Start),
            // [11] Business Intelligence – $69k, 112d → 3 members, avg 3.7yr
            MkPM(completedEntities[11].Proj.Id, empVictor.Id, "Data Analyst", 40m, cpData[11].Start),
            MkPM(completedEntities[11].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[11].Start),
            MkPM(completedEntities[11].Proj.Id, empCatalin.Id, "QA Engineer", 20m, cpData[11].Start),
            // [12] HR Self-Service Portal – $51k, 84d → 3 members, avg 3.3yr
            MkPM(completedEntities[12].Proj.Id, empMaria.Id, "Full-Stack Dev", 40m, cpData[12].Start),
            MkPM(completedEntities[12].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[12].Start),
            MkPM(completedEntities[12].Proj.Id, empCarmen.Id, "UI Designer", 20m, cpData[12].Start),
            // [13] Supply Chain Dashboard – $33k, 56d → 2 members, avg 4.0yr
            MkPM(completedEntities[13].Proj.Id, empVictor.Id, "Data Analyst", 40m, cpData[13].Start),
            MkPM(completedEntities[13].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[13].Start),
            // [14] E-Commerce Redesign – $50k, 84d → 3 members, avg 3.3yr
            MkPM(completedEntities[14].Proj.Id, empMaria.Id, "Full-Stack Dev", 40m, cpData[14].Start),
            MkPM(completedEntities[14].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[14].Start),
            MkPM(completedEntities[14].Proj.Id, empIoana.Id, "Junior Developer", 20m, cpData[14].Start),
            // [15] Inventory Management – $42k, 70d → 2 members, avg 3.0yr
            MkPM(completedEntities[15].Proj.Id, empAndrei.Id, "Backend Developer", 40m, cpData[15].Start),
            MkPM(completedEntities[15].Proj.Id, empIoana.Id, "Junior Developer", 40m, cpData[15].Start),
            // [16] Customer Loyalty App – $32k, 56d → 2 members, avg 2.0yr
            MkPM(completedEntities[16].Proj.Id, empJames.Id, "Senior Developer", 40m, cpData[16].Start),
            MkPM(completedEntities[16].Proj.Id, empLisa.Id, "Frontend Developer", 40m, cpData[16].Start),
            // [17] Appointment Management – $64k, 112d → 3 members, avg 4.0yr
            MkPM(completedEntities[17].Proj.Id, empAlex.Id, "Lead Developer", 40m, cpData[17].Start),
            MkPM(completedEntities[17].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[17].Start),
            MkPM(completedEntities[17].Proj.Id, empCarmen.Id, "UI Designer", 20m, cpData[17].Start),
            // [18] SaaS Platform Upgrade – $51k, 84d → 3 members, avg 3.5yr
            MkPM(completedEntities[18].Proj.Id, empAndrei.Id, "Backend Developer", 40m, cpData[18].Start),
            MkPM(completedEntities[18].Proj.Id, empBogdan.Id, "DevOps Engineer", 40m, cpData[18].Start),
            MkPM(completedEntities[18].Proj.Id, empLisa.Id, "Frontend Developer", 40m, cpData[18].Start),
            // [19] Analytics Dashboard v2 – $37k, 70d → 2 members, avg 3.5yr
            MkPM(completedEntities[19].Proj.Id, empVictor.Id, "Data Analyst", 40m, cpData[19].Start),
            MkPM(completedEntities[19].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[19].Start),
            // [20] Electronic Health Records – $187k, 174d → 5 members, avg 4.8yr (over-budget)
            MkPM(completedEntities[20].Proj.Id, empAlex.Id, "Lead Developer", 40m, cpData[20].Start),
            MkPM(completedEntities[20].Proj.Id, empMaria.Id, "Full-Stack Dev", 40m, cpData[20].Start),
            MkPM(completedEntities[20].Proj.Id, empAndrei.Id, "Backend Developer", 40m, cpData[20].Start),
            MkPM(completedEntities[20].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[20].Start),
            MkPM(completedEntities[20].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[20].Start),
            // [21] Microservices Migration – $97k, 116d → 4 members, avg 4.25yr (over-budget)
            MkPM(completedEntities[21].Proj.Id, empAlex.Id, "Lead Developer", 40m, cpData[21].Start),
            MkPM(completedEntities[21].Proj.Id, empBogdan.Id, "DevOps Engineer", 40m, cpData[21].Start),
            MkPM(completedEntities[21].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[21].Start),
            MkPM(completedEntities[21].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[21].Start),
            // [22] ML Pipeline Integration – $55k, 131d → 3 members, avg 3.7yr (late)
            MkPM(completedEntities[22].Proj.Id, empVictor.Id, "Data Analyst", 40m, cpData[22].Start),
            MkPM(completedEntities[22].Proj.Id, empAndrei.Id, "Backend Developer", 40m, cpData[22].Start),
            MkPM(completedEntities[22].Proj.Id, empIoana.Id, "Junior Developer", 40m, cpData[22].Start),
            // [23] Research Data Platform – $80k, 165d → 4 members, avg 3.75yr (late)
            MkPM(completedEntities[23].Proj.Id, empMaria.Id, "Full-Stack Dev", 40m, cpData[23].Start),
            MkPM(completedEntities[23].Proj.Id, empVictor.Id, "Data Analyst", 40m, cpData[23].Start),
            MkPM(completedEntities[23].Proj.Id, empCatalin.Id, "QA Engineer", 40m, cpData[23].Start),
            MkPM(completedEntities[23].Proj.Id, empIoana.Id, "Junior Developer", 20m, cpData[23].Start),
            // [24] Project Management System – $76k, 137d → 4 members, avg 3.25yr (late + over-budget)
            MkPM(completedEntities[24].Proj.Id, empAlex.Id, "Lead Developer", 40m, cpData[24].Start),
            MkPM(completedEntities[24].Proj.Id, empElena.Id, "Full-Stack Dev", 40m, cpData[24].Start),
            MkPM(completedEntities[24].Proj.Id, empRadu.Id, "Backend Developer", 40m, cpData[24].Start),
            MkPM(completedEntities[24].Proj.Id, empCarmen.Id, "UI Designer", 20m, cpData[24].Start)
        );

        await db.SaveChangesAsync(cancellationToken);

        // Historical time entries for completed projects (all dated well before 90-day window)
        for (int i = 0; i < cpData.Length; i++)
        {
            var (proj, implTask) = completedEntities[i];
            var cp = cpData[i];
            bool atRisk = cp.ActBudget > cp.EstBudget || cp.ActDays > cp.EstDays;
            // timeBurnRate = actualHours / 160 (estimated)
            // on-track: ~0.875 (140h), over-budget: ~1.22 (195h), late: ~1.06 (170h)
            decimal hrs = atRisk ? (cp.ActBudget > cp.EstBudget ? 195m : 170m) : 140m;
            db.TimeEntries.Add(new TimeEntry
            {
                Id = Guid.NewGuid(),
                ProjectId = proj.Id,
                TaskId = implTask.Id,
                EmployeeId = empMichael.Id,
                Date = proj.EndDate!.Value.AddDays(-10),  // historical, before 90-day window
                HoursLogged = hrs,
                Description = $"Development – {cp.Name}",
            });
        }
        await db.SaveChangesAsync(cancellationToken);

        // ── 10. Active Projects (5) ───────────────────────────────────────────────
        // Risk profiles: A=OnTrack, B=Watch, C=OnTrack, D=Watch, E=AtRisk

        var projA = new Project { Id = Guid.NewGuid(), Name = "E-Commerce Platform", ClientId = clientTech.Id, ProjectManagerId = michaelUser.Id, Status = ProjectStatus.Active, StartDate = new DateOnly(2025, 11, 1), EndDate = new DateOnly(2026, 8, 31), EstimatedBudget = 250_000m, ActualBudget = 80_000m, EstimatedDurationDays = 303 };
        var projB = new Project { Id = Guid.NewGuid(), Name = "Trading Dashboard", ClientId = clientFinance.Id, ProjectManagerId = michaelUser.Id, Status = ProjectStatus.Active, StartDate = new DateOnly(2025, 9, 15), EndDate = new DateOnly(2026, 4, 30), EstimatedBudget = 160_000m, ActualBudget = 120_000m, EstimatedDurationDays = 227 };
        var projC = new Project { Id = Guid.NewGuid(), Name = "ERP System", ClientId = clientInnovatech.Id, ProjectManagerId = michaelUser.Id, Status = ProjectStatus.Active, StartDate = new DateOnly(2026, 1, 10), EndDate = new DateOnly(2026, 12, 31), EstimatedBudget = 300_000m, ActualBudget = 30_000m, EstimatedDurationDays = 355 };
        var projD = new Project { Id = Guid.NewGuid(), Name = "Patient Portal", ClientId = clientMedSoft.Id, ProjectManagerId = michaelUser.Id, Status = ProjectStatus.Active, StartDate = new DateOnly(2025, 8, 1), EndDate = new DateOnly(2026, 4, 30), EstimatedBudget = 130_000m, ActualBudget = 110_000m, EstimatedDurationDays = 272 };
        var projE = new Project { Id = Guid.NewGuid(), Name = "Mobile App", ClientId = clientRetailPro.Id, ProjectManagerId = michaelUser.Id, Status = ProjectStatus.Active, StartDate = new DateOnly(2025, 7, 1), EndDate = new DateOnly(2026, 3, 31), EstimatedBudget = 70_000m, ActualBudget = 85_000m, EstimatedDurationDays = 273 };

        db.Projects.AddRange(projA, projB, projC, projD, projE);
        await db.SaveChangesAsync(cancellationToken);

        // ── 11. Project Members ───────────────────────────────────────────────────

        db.ProjectMembers.AddRange(
            // projA — TechCorp E-Commerce
            MkPM(projA.Id, empAlex.Id, "Lead Developer", 40m, new DateOnly(2025, 11, 1)),
            MkPM(projA.Id, empMaria.Id, "Frontend Dev", 40m, new DateOnly(2025, 11, 1)),
            MkPM(projA.Id, empElena.Id, "Backend Dev", 40m, new DateOnly(2025, 11, 1)),
            MkPM(projA.Id, empIoana.Id, "Junior Dev", 20m, new DateOnly(2025, 11, 1)),
            MkPM(projA.Id, empMichael.Id, "Project Manager", 10m, new DateOnly(2025, 11, 1)),
            // projB — Finance Trading Dashboard
            MkPM(projB.Id, empAndrei.Id, "Backend Developer", 40m, new DateOnly(2025, 9, 15)),
            MkPM(projB.Id, empVictor.Id, "Data Analyst", 40m, new DateOnly(2025, 9, 15)),
            MkPM(projB.Id, empCarmen.Id, "UI Designer", 10m, new DateOnly(2025, 9, 15)),
            MkPM(projB.Id, empMichael.Id, "Project Manager", 10m, new DateOnly(2025, 9, 15)),
            // projC — Innovatech ERP
            MkPM(projC.Id, empAlex.Id, "Solution Architect", 40m, new DateOnly(2026, 1, 10)),
            MkPM(projC.Id, empCatalin.Id, "QA Engineer", 40m, new DateOnly(2026, 1, 10)),
            MkPM(projC.Id, empBogdan.Id, "DevOps Engineer", 20m, new DateOnly(2026, 1, 10)),
            MkPM(projC.Id, empMichael.Id, "Project Manager", 10m, new DateOnly(2026, 1, 10)),
            // projD — MedSoft Patient Portal
            MkPM(projD.Id, empMaria.Id, "Frontend Dev", 40m, new DateOnly(2025, 8, 1)),
            MkPM(projD.Id, empRadu.Id, "Backend Developer", 40m, new DateOnly(2025, 8, 1)),
            MkPM(projD.Id, empMichael.Id, "Project Manager", 10m, new DateOnly(2025, 8, 1)),
            // projE — RetailPro Mobile App
            MkPM(projE.Id, empJames.Id, "Senior Developer", 40m, new DateOnly(2025, 7, 1)),
            MkPM(projE.Id, empLisa.Id, "Frontend Dev", 40m, new DateOnly(2025, 7, 1)),
            MkPM(projE.Id, empAndrei.Id, "Backend Developer", 40m, new DateOnly(2025, 7, 1)),
            MkPM(projE.Id, empMichael.Id, "Project Manager", 10m, new DateOnly(2025, 7, 1))
        );
        await db.SaveChangesAsync(cancellationToken);

        // ── 12. Kanban Boards & Columns ───────────────────────────────────────────

        var boardA = new KanbanBoard { Id = Guid.NewGuid(), ProjectId = projA.Id };
        var boardB = new KanbanBoard { Id = Guid.NewGuid(), ProjectId = projB.Id };
        var boardC = new KanbanBoard { Id = Guid.NewGuid(), ProjectId = projC.Id };
        var boardD = new KanbanBoard { Id = Guid.NewGuid(), ProjectId = projD.Id };
        var boardE = new KanbanBoard { Id = Guid.NewGuid(), ProjectId = projE.Id };
        db.KanbanBoards.AddRange(boardA, boardB, boardC, boardD, boardE);
        await db.SaveChangesAsync(cancellationToken);

        var (aBack, aInP, aRev, aDone) = Cols(boardA.Id);
        var (bBack, bInP, bRev, bDone) = Cols(boardB.Id);
        var (cBack, cInP, cRev, cDone) = Cols(boardC.Id);
        var (dBack, dInP, dRev, dDone) = Cols(boardD.Id);
        var (eBack, eInP, eRev, eDone) = Cols(boardE.Id);
        db.KanbanColumns.AddRange(
            aBack, aInP, aRev, aDone,
            bBack, bInP, bRev, bDone,
            cBack, cInP, cRev, cDone,
            dBack, dInP, dRev, dDone,
            eBack, eInP, eRev, eDone);
        await db.SaveChangesAsync(cancellationToken);

        // ── 13. Tasks (active projects) ───────────────────────────────────────────

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var over20d = today.AddDays(-20);
        var over8d = today.AddDays(-8);
        var over3d = today.AddDays(-3);

        // ─ Project A: TechCorp E-Commerce (OnTrack) — 15 done, 4 in-progress, 5 backlog ─
        var aDoneTasks = new[]
        {
            MkTask(projA.Id,"Auth & JWT implementation",     TaskPriority.Critical, TaskStatus.Done,       16m, michaelUser.Id, empAlex.Id,  aDone.Id),
            MkTask(projA.Id,"Database schema design",        TaskPriority.High,     TaskStatus.Done,       12m, michaelUser.Id, empAlex.Id,  aDone.Id),
            MkTask(projA.Id,"Product catalog API",           TaskPriority.High,     TaskStatus.Done,       20m, michaelUser.Id, empAlex.Id,  aDone.Id),
            MkTask(projA.Id,"Cart & checkout API",           TaskPriority.High,     TaskStatus.Done,       24m, michaelUser.Id, empAlex.Id,  aDone.Id),
            MkTask(projA.Id,"Order management service",      TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empAlex.Id,  aDone.Id),
            MkTask(projA.Id,"Payment gateway integration",   TaskPriority.Critical, TaskStatus.Done,       20m, michaelUser.Id, empAlex.Id,  aDone.Id),
            MkTask(projA.Id,"Search & filter service",       TaskPriority.Medium,   TaskStatus.Done,       12m, michaelUser.Id, empAlex.Id,  aDone.Id),
            MkTask(projA.Id,"Admin dashboard API",           TaskPriority.Medium,   TaskStatus.Done,        8m, michaelUser.Id, empAlex.Id,  aDone.Id),
            MkTask(projA.Id,"Homepage component",            TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empMaria.Id, aDone.Id),
            MkTask(projA.Id,"Product listing component",     TaskPriority.High,     TaskStatus.Done,       12m, michaelUser.Id, empMaria.Id, aDone.Id),
            MkTask(projA.Id,"Cart component",                TaskPriority.High,     TaskStatus.Done,       10m, michaelUser.Id, empMaria.Id, aDone.Id),
            MkTask(projA.Id,"Checkout flow UI",              TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empMaria.Id, aDone.Id),
            MkTask(projA.Id,"Email notification service",    TaskPriority.Medium,   TaskStatus.Done,        8m, michaelUser.Id, empElena.Id, aDone.Id),
            MkTask(projA.Id,"Inventory sync service",        TaskPriority.Medium,   TaskStatus.Done,       12m, michaelUser.Id, empElena.Id, aDone.Id),
            MkTask(projA.Id,"Setup CI/CD pipeline",          TaskPriority.Low,      TaskStatus.Done,        8m, michaelUser.Id, empIoana.Id, aDone.Id),
        };
        db.Tasks.AddRange(aDoneTasks);
        db.Tasks.AddRange(
            MkTask(projA.Id, "User reviews system", TaskPriority.Medium, TaskStatus.InProgress, 16m, michaelUser.Id, empAlex.Id, aInP.Id),
            MkTask(projA.Id, "Product detail page", TaskPriority.Medium, TaskStatus.InProgress, 12m, michaelUser.Id, empMaria.Id, aInP.Id),
            MkTask(projA.Id, "Analytics dashboard", TaskPriority.Low, TaskStatus.InProgress, 20m, michaelUser.Id, empElena.Id, aInP.Id),
            MkTask(projA.Id, "Unit test coverage", TaskPriority.Medium, TaskStatus.InProgress, 16m, michaelUser.Id, empIoana.Id, aInP.Id),
            MkTask(projA.Id, "Mobile responsive optimization", TaskPriority.Medium, TaskStatus.Backlog, 12m, michaelUser.Id),
            MkTask(projA.Id, "SEO metadata service", TaskPriority.Low, TaskStatus.Backlog, 8m, michaelUser.Id),
            MkTask(projA.Id, "Multi-language support", TaskPriority.Low, TaskStatus.Backlog, 24m, michaelUser.Id),
            MkTask(projA.Id, "Performance profiling", TaskPriority.Medium, TaskStatus.Backlog, 8m, michaelUser.Id),
            MkTask(projA.Id, "Load testing", TaskPriority.Medium, TaskStatus.Backlog, 12m, michaelUser.Id)
        );

        // ─ Project B: Finance Trading Dashboard (Watch — near deadline, 2 overdue) ─
        var bDoneTasks = new[]
        {
            MkTask(projB.Id,"Market data ingestion service", TaskPriority.Critical, TaskStatus.Done,       24m, michaelUser.Id, empAndrei.Id, bDone.Id),
            MkTask(projB.Id,"Real-time WebSocket API",       TaskPriority.Critical, TaskStatus.Done,       20m, michaelUser.Id, empAndrei.Id, bDone.Id),
            MkTask(projB.Id,"Portfolio positions engine",    TaskPriority.High,     TaskStatus.Done,       24m, michaelUser.Id, empAndrei.Id, bDone.Id),
            MkTask(projB.Id,"Trade history service",         TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empAndrei.Id, bDone.Id),
            MkTask(projB.Id,"Risk calculation engine",       TaskPriority.Critical, TaskStatus.Done,       32m, michaelUser.Id, empAndrei.Id, bDone.Id),
            MkTask(projB.Id,"Authentication & RBAC",         TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empAndrei.Id, bDone.Id),
            MkTask(projB.Id,"Audit logging service",         TaskPriority.Medium,   TaskStatus.Done,       12m, michaelUser.Id, empAndrei.Id, bDone.Id),
            MkTask(projB.Id,"Dashboard charting components", TaskPriority.High,     TaskStatus.Done,       24m, michaelUser.Id, empVictor.Id, bDone.Id),
            MkTask(projB.Id,"Portfolio overview widget",     TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empVictor.Id, bDone.Id),
            MkTask(projB.Id,"Heatmap component",             TaskPriority.Medium,   TaskStatus.Done,       12m, michaelUser.Id, empVictor.Id, bDone.Id),
            MkTask(projB.Id,"P&L report component",          TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empVictor.Id, bDone.Id),
            MkTask(projB.Id,"Alert management UI",           TaskPriority.Medium,   TaskStatus.Done,       12m, michaelUser.Id, empVictor.Id, bDone.Id),
            MkTask(projB.Id,"UI design system",              TaskPriority.Medium,   TaskStatus.Done,       20m, michaelUser.Id, empCarmen.Id, bDone.Id),
        };
        db.Tasks.AddRange(bDoneTasks);
        db.Tasks.AddRange(
            MkTask(projB.Id, "Export to CSV/Excel", TaskPriority.Medium, TaskStatus.InProgress, 12m, michaelUser.Id, empAndrei.Id, bInP.Id),
            MkTask(projB.Id, "Mobile responsive layout", TaskPriority.Medium, TaskStatus.InProgress, 16m, michaelUser.Id, empVictor.Id, bInP.Id),
            MkTask(projB.Id, "Notification preferences", TaskPriority.Low, TaskStatus.InProgress, 8m, michaelUser.Id, empVictor.Id, bInP.Id),
            MkTask(projB.Id, "Performance optimization", TaskPriority.High, TaskStatus.InProgress, 16m, michaelUser.Id, empAndrei.Id, bInP.Id),
            MkTask(projB.Id, "E2E test suite", TaskPriority.High, TaskStatus.InProgress, 20m, michaelUser.Id, empAndrei.Id, bInP.Id),
            MkTask(projB.Id, "Data archive job", TaskPriority.High, TaskStatus.InProgress, 12m, michaelUser.Id, empAndrei.Id, bInP.Id, over20d),  // overdue
            MkTask(projB.Id, "Compliance report generator", TaskPriority.Critical, TaskStatus.InProgress, 20m, michaelUser.Id, empVictor.Id, bInP.Id, over8d),   // overdue
            MkTask(projB.Id, "Dark mode theme", TaskPriority.Low, TaskStatus.Backlog, 8m, michaelUser.Id),
            MkTask(projB.Id, "Custom alert thresholds", TaskPriority.Low, TaskStatus.Backlog, 8m, michaelUser.Id),
            MkTask(projB.Id, "User onboarding tour", TaskPriority.Low, TaskStatus.Backlog, 6m, michaelUser.Id)
        );

        // ─ Project C: Innovatech ERP (OnTrack — early stage) — 11 done, 3 in-progress, 10 backlog ─
        var cDoneTasks = new[]
        {
            MkTask(projC.Id,"Solution architecture design",  TaskPriority.Critical, TaskStatus.Done,       32m, michaelUser.Id, empAlex.Id,    cDone.Id),
            MkTask(projC.Id,"Database & schema design",      TaskPriority.Critical, TaskStatus.Done,       24m, michaelUser.Id, empAlex.Id,    cDone.Id),
            MkTask(projC.Id,"Core domain models",            TaskPriority.High,     TaskStatus.Done,       20m, michaelUser.Id, empAlex.Id,    cDone.Id),
            MkTask(projC.Id,"API foundation & middleware",   TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empAlex.Id,    cDone.Id),
            MkTask(projC.Id,"Authentication module",         TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empAlex.Id,    cDone.Id),
            MkTask(projC.Id,"QA test strategy document",     TaskPriority.Medium,   TaskStatus.Done,       12m, michaelUser.Id, empCatalin.Id, cDone.Id),
            MkTask(projC.Id,"Automated test framework setup",TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empCatalin.Id, cDone.Id),
            MkTask(projC.Id,"Integration test suite v1",     TaskPriority.Medium,   TaskStatus.Done,       16m, michaelUser.Id, empCatalin.Id, cDone.Id),
            MkTask(projC.Id,"API contract tests",            TaskPriority.Medium,   TaskStatus.Done,       12m, michaelUser.Id, empCatalin.Id, cDone.Id),
            MkTask(projC.Id,"Kubernetes cluster setup",      TaskPriority.High,     TaskStatus.Done,       20m, michaelUser.Id, empBogdan.Id,  cDone.Id),
            MkTask(projC.Id,"CI/CD pipeline",                TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empBogdan.Id,  cDone.Id),
        };
        db.Tasks.AddRange(cDoneTasks);
        db.Tasks.AddRange(
            MkTask(projC.Id, "HR module – employee CRUD", TaskPriority.High, TaskStatus.InProgress, 24m, michaelUser.Id, empAlex.Id, cInP.Id),
            MkTask(projC.Id, "Payroll calculation engine", TaskPriority.High, TaskStatus.InProgress, 32m, michaelUser.Id, empCatalin.Id, cInP.Id),
            MkTask(projC.Id, "Docker image optimization", TaskPriority.Medium, TaskStatus.InProgress, 12m, michaelUser.Id, empBogdan.Id, cInP.Id),
            MkTask(projC.Id, "Finance module – GL", TaskPriority.High, TaskStatus.Backlog, 40m, michaelUser.Id),
            MkTask(projC.Id, "Finance module – AP/AR", TaskPriority.High, TaskStatus.Backlog, 40m, michaelUser.Id),
            MkTask(projC.Id, "Inventory module", TaskPriority.High, TaskStatus.Backlog, 48m, michaelUser.Id),
            MkTask(projC.Id, "Procurement module", TaskPriority.Medium, TaskStatus.Backlog, 40m, michaelUser.Id),
            MkTask(projC.Id, "Reporting & analytics module", TaskPriority.Medium, TaskStatus.Backlog, 48m, michaelUser.Id),
            MkTask(projC.Id, "CRM integration module", TaskPriority.Low, TaskStatus.Backlog, 32m, michaelUser.Id),
            MkTask(projC.Id, "Mobile app foundation", TaskPriority.Low, TaskStatus.Backlog, 48m, michaelUser.Id),
            MkTask(projC.Id, "E2E test coverage", TaskPriority.Medium, TaskStatus.Backlog, 32m, michaelUser.Id),
            MkTask(projC.Id, "Load testing suite", TaskPriority.Medium, TaskStatus.Backlog, 16m, michaelUser.Id),
            MkTask(projC.Id, "Deployment automation", TaskPriority.Low, TaskStatus.Backlog, 16m, michaelUser.Id)
        );

        // ─ Project D: MedSoft Patient Portal (Watch — 79% timeline, 2 overdue) ─
        var dDoneTasks = new[]
        {
            MkTask(projD.Id,"Patient registration UI",       TaskPriority.High,     TaskStatus.Done,       20m, michaelUser.Id, empMaria.Id, dDone.Id),
            MkTask(projD.Id,"Appointment booking flow",      TaskPriority.High,     TaskStatus.Done,       24m, michaelUser.Id, empMaria.Id, dDone.Id),
            MkTask(projD.Id,"Doctor schedule UI",            TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empMaria.Id, dDone.Id),
            MkTask(projD.Id,"Notification UI",               TaskPriority.Medium,   TaskStatus.Done,       12m, michaelUser.Id, empMaria.Id, dDone.Id),
            MkTask(projD.Id,"Patient REST API",              TaskPriority.Critical, TaskStatus.Done,       24m, michaelUser.Id, empRadu.Id,  dDone.Id),
            MkTask(projD.Id,"Appointment REST API",          TaskPriority.Critical, TaskStatus.Done,       20m, michaelUser.Id, empRadu.Id,  dDone.Id),
            MkTask(projD.Id,"HL7 FHIR integration",          TaskPriority.High,     TaskStatus.Done,       32m, michaelUser.Id, empRadu.Id,  dDone.Id),
            MkTask(projD.Id,"Audit & compliance logging",    TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empRadu.Id,  dDone.Id),
        };
        db.Tasks.AddRange(dDoneTasks);
        db.Tasks.AddRange(
            MkTask(projD.Id, "Medical record viewer", TaskPriority.High, TaskStatus.InProgress, 24m, michaelUser.Id, empMaria.Id, dInP.Id),
            MkTask(projD.Id, "Prescription management API", TaskPriority.High, TaskStatus.InProgress, 20m, michaelUser.Id, empRadu.Id, dInP.Id),
            MkTask(projD.Id, "E2E encryption for PHI", TaskPriority.Critical, TaskStatus.InProgress, 24m, michaelUser.Id, empRadu.Id, dInP.Id),
            MkTask(projD.Id, "Admin dashboard", TaskPriority.Medium, TaskStatus.InProgress, 16m, michaelUser.Id, empMaria.Id, dInP.Id),
            MkTask(projD.Id, "Lab results integration", TaskPriority.High, TaskStatus.InProgress, 24m, michaelUser.Id, empRadu.Id, dInP.Id, over20d),  // overdue
            MkTask(projD.Id, "GDPR consent management", TaskPriority.Critical, TaskStatus.InProgress, 16m, michaelUser.Id, empMaria.Id, dInP.Id, over8d),   // overdue
            MkTask(projD.Id, "Telemedicine video call", TaskPriority.Medium, TaskStatus.Backlog, 32m, michaelUser.Id),
            MkTask(projD.Id, "Patient feedback survey", TaskPriority.Low, TaskStatus.Backlog, 12m, michaelUser.Id)
        );

        // ─ Project E: RetailPro Mobile App (AtRisk — over budget, 90% timeline, 3 overdue) ─
        var eDoneTasks = new[]
        {
            MkTask(projE.Id,"REST API integration layer",    TaskPriority.High,     TaskStatus.Done,       24m, michaelUser.Id, empAndrei.Id, eDone.Id),
            MkTask(projE.Id,"Auth service (OAuth2)",         TaskPriority.Critical, TaskStatus.Done,       16m, michaelUser.Id, empAndrei.Id, eDone.Id),
            MkTask(projE.Id,"Push notification service",     TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empAndrei.Id, eDone.Id),
            MkTask(projE.Id,"Product browsing screen",       TaskPriority.High,     TaskStatus.Done,       20m, michaelUser.Id, empJames.Id,  eDone.Id),
            MkTask(projE.Id,"Cart & wishlist screens",       TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empJames.Id,  eDone.Id),
            MkTask(projE.Id,"Order tracking screen",         TaskPriority.High,     TaskStatus.Done,       12m, michaelUser.Id, empJames.Id,  eDone.Id),
            MkTask(projE.Id,"Login & registration screens",  TaskPriority.High,     TaskStatus.Done,       16m, michaelUser.Id, empLisa.Id,   eDone.Id),
            MkTask(projE.Id,"UI component library",          TaskPriority.Medium,   TaskStatus.Done,       20m, michaelUser.Id, empLisa.Id,   eDone.Id),
        };
        db.Tasks.AddRange(eDoneTasks);
        db.Tasks.AddRange(
            MkTask(projE.Id, "Checkout payment flow", TaskPriority.Critical, TaskStatus.InProgress, 24m, michaelUser.Id, empAndrei.Id, eInP.Id),
            MkTask(projE.Id, "Store locator feature", TaskPriority.Medium, TaskStatus.InProgress, 12m, michaelUser.Id, empJames.Id, eInP.Id),
            MkTask(projE.Id, "Loyalty points UI", TaskPriority.Medium, TaskStatus.InProgress, 12m, michaelUser.Id, empLisa.Id, eInP.Id),
            MkTask(projE.Id, "Performance profiling", TaskPriority.High, TaskStatus.InProgress, 12m, michaelUser.Id, empJames.Id, eInP.Id),
            MkTask(projE.Id, "Offline mode / caching", TaskPriority.High, TaskStatus.InProgress, 20m, michaelUser.Id, empAndrei.Id, eInP.Id),
            MkTask(projE.Id, "App Store submission", TaskPriority.Critical, TaskStatus.InProgress, 8m, michaelUser.Id, empJames.Id, eInP.Id, over20d),  // overdue
            MkTask(projE.Id, "Crash reporting integration", TaskPriority.High, TaskStatus.InProgress, 8m, michaelUser.Id, empAndrei.Id, eInP.Id, over8d),   // overdue
            MkTask(projE.Id, "Accessibility audit (WCAG)", TaskPriority.High, TaskStatus.InProgress, 12m, michaelUser.Id, empLisa.Id, eInP.Id, over3d),   // overdue
            MkTask(projE.Id, "A/B test framework", TaskPriority.Low, TaskStatus.Backlog, 12m, michaelUser.Id),
            MkTask(projE.Id, "Analytics SDK integration", TaskPriority.Medium, TaskStatus.Backlog, 8m, michaelUser.Id),
            MkTask(projE.Id, "Widget / shortcut support", TaskPriority.Low, TaskStatus.Backlog, 16m, michaelUser.Id)
        );
        await db.SaveChangesAsync(cancellationToken);

        // ── 14. Time Entries (last 90 days) ───────────────────────────────────────
        // IMPORTANT: Dates are relative to DateTime.UtcNow so they always stay
        // within the 90-day clustering window regardless of when the demo runs
        // (tested Jan 2026, presented June 2026 — all dates remain valid).
        // High Performers: Alex ~192h, Maria ~175h, Andrei ~168h
        // Standard:        Victor ~75h, Elena ~72h, Catalin ~62h, Radu ~58h, James ~52h, Michael ~28h
        // Low Utilisation: Bogdan ~18h, Lisa ~16h, Ioana ~14h, Carmen ~10h | Sarah & Emily 0h

        var te1 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-85)); // ~12 weeks ago
        var te2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-75)); // ~11 weeks
        var te3 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-63)); // ~9 weeks
        var te4 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-50)); // ~7 weeks
        var te5 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-38)); // ~5½ weeks
        var te6 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-25)); // ~3½ weeks
        var te7 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)); // ~2 weeks
        var te8 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));  // ~1 week

        // Pick representative tasks to link time entries to
        var aPivot = aDoneTasks[2];  // "Product catalog API"
        var bPivot = bDoneTasks[0];  // "Market data ingestion service"
        var cPivot = cDoneTasks[0];  // "Solution architecture design"
        var dPivot = dDoneTasks[0];  // "Patient registration UI"
        var ePivot = eDoneTasks[0];  // "REST API integration layer"

        // ── HIGH PERFORMERS ────────────────────────────────────────────────────────
        // Alex: 6 entries × 2 projects ≈ 192h
        db.TimeEntries.AddRange(
            TE(projA.Id, aPivot.Id, empAlex.Id, te1, 35m, "Backend auth & catalog APIs"),
            TE(projC.Id, cPivot.Id, empAlex.Id, te2, 35m, "ERP architecture & core modules"),
            TE(projA.Id, aPivot.Id, empAlex.Id, te3, 32m, "Payment & order services"),
            TE(projC.Id, cPivot.Id, empAlex.Id, te5, 32m, "HR module implementation"),
            TE(projA.Id, aPivot.Id, empAlex.Id, te6, 28m, "User reviews & search services"),
            TE(projC.Id, cPivot.Id, empAlex.Id, te8, 30m, "ERP payroll engine design")
        );
        // Maria: 6 entries × 2 projects ≈ 175h
        db.TimeEntries.AddRange(
            TE(projA.Id, aPivot.Id, empMaria.Id, te1, 30m, "Homepage & product listing components"),
            TE(projD.Id, dPivot.Id, empMaria.Id, te2, 32m, "Patient & appointment UI"),
            TE(projA.Id, aPivot.Id, empMaria.Id, te4, 28m, "Cart & checkout UI"),
            TE(projD.Id, dPivot.Id, empMaria.Id, te5, 30m, "Doctor schedule & notification UI"),
            TE(projA.Id, aPivot.Id, empMaria.Id, te6, 27m, "Product detail & reviews UI"),
            TE(projD.Id, dPivot.Id, empMaria.Id, te7, 28m, "Medical record viewer")
        );
        // Andrei: 6 entries × 2 projects ≈ 168h
        db.TimeEntries.AddRange(
            TE(projB.Id, bPivot.Id, empAndrei.Id, te1, 32m, "Market data & WebSocket API"),
            TE(projE.Id, ePivot.Id, empAndrei.Id, te2, 28m, "API integration & auth service"),
            TE(projB.Id, bPivot.Id, empAndrei.Id, te3, 30m, "Risk engine & portfolio positions"),
            TE(projE.Id, ePivot.Id, empAndrei.Id, te5, 25m, "Push notifications & checkout payment"),
            TE(projB.Id, bPivot.Id, empAndrei.Id, te6, 28m, "Compliance report & export features"),
            TE(projE.Id, ePivot.Id, empAndrei.Id, te8, 25m, "Offline mode & crash reporting")
        );

        // ── STANDARD TIER ─────────────────────────────────────────────────────────
        // Victor: 4 entries ≈ 75h
        db.TimeEntries.AddRange(
            TE(projB.Id, bPivot.Id, empVictor.Id, te2, 22m, "Dashboard charts & portfolio widget"),
            TE(projB.Id, bPivot.Id, empVictor.Id, te4, 20m, "Heatmap & P&L components"),
            TE(projB.Id, bPivot.Id, empVictor.Id, te6, 18m, "Alert UI & mobile layout"),
            TE(projB.Id, bPivot.Id, empVictor.Id, te8, 15m, "Notification preferences")
        );
        // Elena: 4 entries ≈ 72h
        db.TimeEntries.AddRange(
            TE(projA.Id, aPivot.Id, empElena.Id, te2, 20m, "Email & inventory sync services"),
            TE(projA.Id, aPivot.Id, empElena.Id, te4, 18m, "Analytics backend service"),
            TE(projA.Id, aPivot.Id, empElena.Id, te6, 18m, "Unit test support"),
            TE(projA.Id, aPivot.Id, empElena.Id, te8, 16m, "Backend optimization")
        );
        // Catalin: 3 entries ≈ 62h
        db.TimeEntries.AddRange(
            TE(projC.Id, cPivot.Id, empCatalin.Id, te3, 22m, "Test strategy & framework setup"),
            TE(projC.Id, cPivot.Id, empCatalin.Id, te5, 20m, "Integration tests & API contracts"),
            TE(projC.Id, cPivot.Id, empCatalin.Id, te7, 20m, "Payroll module test coverage")
        );
        // Radu: 3 entries ≈ 58h
        db.TimeEntries.AddRange(
            TE(projD.Id, dPivot.Id, empRadu.Id, te3, 22m, "Patient & appointment REST API"),
            TE(projD.Id, dPivot.Id, empRadu.Id, te5, 20m, "HL7 FHIR integration"),
            TE(projD.Id, dPivot.Id, empRadu.Id, te7, 16m, "Prescription management API")
        );
        // James: 3 entries ≈ 52h
        db.TimeEntries.AddRange(
            TE(projE.Id, ePivot.Id, empJames.Id, te3, 20m, "Product browsing & cart screens"),
            TE(projE.Id, ePivot.Id, empJames.Id, te5, 17m, "Order tracking & performance"),
            TE(projE.Id, ePivot.Id, empJames.Id, te7, 15m, "Store locator feature")
        );
        // Michael: 4 oversight entries ≈ 28h
        db.TimeEntries.AddRange(
            TE(projA.Id, aPivot.Id, empMichael.Id, te2, 8m, "Sprint planning & review"),
            TE(projB.Id, bPivot.Id, empMichael.Id, te4, 7m, "Stakeholder alignment"),
            TE(projE.Id, ePivot.Id, empMichael.Id, te6, 7m, "Risk review & re-planning"),
            TE(projC.Id, cPivot.Id, empMichael.Id, te8, 6m, "ERP project oversight")
        );

        // ── LOW UTILISATION ────────────────────────────────────────────────────────
        // Bogdan: 2 entries ≈ 18h
        db.TimeEntries.AddRange(
            TE(projC.Id, cPivot.Id, empBogdan.Id, te3, 10m, "Kubernetes & Docker setup"),
            TE(projC.Id, cPivot.Id, empBogdan.Id, te7, 8m, "Docker image optimization")
        );
        // Lisa: 2 entries ≈ 16h
        db.TimeEntries.AddRange(
            TE(projE.Id, ePivot.Id, empLisa.Id, te4, 8m, "Login & registration screens"),
            TE(projE.Id, ePivot.Id, empLisa.Id, te6, 8m, "UI component library refinement")
        );
        // Ioana: 2 entries ≈ 14h
        db.TimeEntries.AddRange(
            TE(projA.Id, aPivot.Id, empIoana.Id, te4, 8m, "CI/CD setup assistance"),
            TE(projA.Id, aPivot.Id, empIoana.Id, te7, 6m, "Unit test coverage support")
        );
        // Carmen: 2 entries ≈ 10h
        db.TimeEntries.AddRange(
            TE(projB.Id, bPivot.Id, empCarmen.Id, te4, 6m, "UI design system components"),
            TE(projE.Id, ePivot.Id, empCarmen.Id, te7, 4m, "Mobile app UI assets")
        );
        // Sarah, Emily: intentionally 0 entries (HR/Sales — not in project delivery teams)
        await db.SaveChangesAsync(cancellationToken);

        // ── 15. Invoices ──────────────────────────────────────────────────────────
        // Client segmentation scoring formula: totalRevenue - avgDelayDays*100 - openAmount*0.5
        // Key Account: ~200-280k revenue, early/on-time payment, low open
        // Growth:      ~40-75k revenue, 5-13 day delay, moderate open
        // At Risk:     ~15-22k revenue, 35-62 day delay, high open

        int invNum = 1;
        string InvNo() => $"INV-{invNum++:D4}";

        // TechCorp (Key Account) — 280k paid, avg -3 days, 25k open
        db.Invoices.AddRange(
            PaidInv(InvNo(), clientTech.Id, new DateOnly(2023, 3, 1), 42_000m, 0.19m, -5),
            PaidInv(InvNo(), clientTech.Id, new DateOnly(2023, 7, 1), 45_000m, 0.19m, -3),
            PaidInv(InvNo(), clientTech.Id, new DateOnly(2024, 2, 1), 48_000m, 0.19m, -4),
            PaidInv(InvNo(), clientTech.Id, new DateOnly(2024, 7, 1), 52_000m, 0.19m, -2),
            PaidInv(InvNo(), clientTech.Id, new DateOnly(2025, 1, 1), 38_000m, 0.19m, 0),
            PaidInv(InvNo(), clientTech.Id, new DateOnly(2025, 6, 1), 55_000m, 0.19m, -3),
            OpenInv(InvNo(), clientTech.Id, new DateOnly(2026, 1, 15), 25_000m, 0.19m)
        );
        // Global Finance (Key Account) — 180k paid, avg +2 days, 18k open
        db.Invoices.AddRange(
            PaidInv(InvNo(), clientFinance.Id, new DateOnly(2023, 5, 1), 38_000m, 0.19m, 3),
            PaidInv(InvNo(), clientFinance.Id, new DateOnly(2023, 12, 1), 42_000m, 0.19m, 1),
            PaidInv(InvNo(), clientFinance.Id, new DateOnly(2024, 6, 1), 55_000m, 0.19m, 2),
            PaidInv(InvNo(), clientFinance.Id, new DateOnly(2025, 3, 1), 45_000m, 0.19m, 2),
            OpenInv(InvNo(), clientFinance.Id, new DateOnly(2026, 1, 15), 18_000m, 0.19m)
        );
        // Innovatech (Key Account) — 220k paid, avg 0 days, 30k open
        db.Invoices.AddRange(
            PaidInv(InvNo(), clientInnovatech.Id, new DateOnly(2023, 4, 1), 45_000m, 0.19m, 0),
            PaidInv(InvNo(), clientInnovatech.Id, new DateOnly(2023, 10, 1), 50_000m, 0.19m, -1),
            PaidInv(InvNo(), clientInnovatech.Id, new DateOnly(2024, 4, 1), 62_000m, 0.19m, 0),
            PaidInv(InvNo(), clientInnovatech.Id, new DateOnly(2025, 1, 1), 63_000m, 0.19m, 1),
            OpenInv(InvNo(), clientInnovatech.Id, new DateOnly(2026, 1, 15), 15_000m, 0.19m),
            OpenInv(InvNo(), clientInnovatech.Id, new DateOnly(2026, 2, 15), 15_000m, 0.19m)
        );
        // RetailPro (Growth) — 75k paid, avg +8 days, 15k open
        db.Invoices.AddRange(
            PaidInv(InvNo(), clientRetailPro.Id, new DateOnly(2023, 7, 1), 20_000m, 0.19m, 8),
            PaidInv(InvNo(), clientRetailPro.Id, new DateOnly(2024, 6, 1), 25_000m, 0.19m, 7),
            PaidInv(InvNo(), clientRetailPro.Id, new DateOnly(2025, 3, 1), 30_000m, 0.19m, 9),
            OpenInv(InvNo(), clientRetailPro.Id, new DateOnly(2026, 1, 15), 15_000m, 0.19m)
        );
        // MedSoft (Growth) — 60k paid, avg +12 days, 20k open
        db.Invoices.AddRange(
            PaidInv(InvNo(), clientMedSoft.Id, new DateOnly(2024, 7, 1), 25_000m, 0.19m, 11),
            PaidInv(InvNo(), clientMedSoft.Id, new DateOnly(2025, 3, 1), 35_000m, 0.19m, 13),
            OpenInv(InvNo(), clientMedSoft.Id, new DateOnly(2026, 1, 15), 20_000m, 0.19m)
        );
        // CloudBridge (Growth) — 50k paid, avg +6 days, 0 open
        db.Invoices.AddRange(
            PaidInv(InvNo(), clientCloudBridge.Id, new DateOnly(2024, 9, 1), 15_000m, 0.19m, 5),
            PaidInv(InvNo(), clientCloudBridge.Id, new DateOnly(2025, 1, 1), 18_000m, 0.19m, 7),
            PaidInv(InvNo(), clientCloudBridge.Id, new DateOnly(2025, 6, 1), 17_000m, 0.19m, 6)
        );
        // DataFusion (Growth) — 40k paid, avg +10 days, 10k open
        db.Invoices.AddRange(
            PaidInv(InvNo(), clientDataFusion.Id, new DateOnly(2024, 6, 1), 18_000m, 0.19m, 10),
            PaidInv(InvNo(), clientDataFusion.Id, new DateOnly(2025, 1, 1), 22_000m, 0.19m, 10),
            OverdueInv(InvNo(), clientDataFusion.Id, new DateOnly(2025, 11, 1), 10_000m, 0.19m)
        );
        // BioNova (At Risk) — 22k paid, avg +35 days, 32k open
        db.Invoices.AddRange(
            PaidInv(InvNo(), clientBioNova.Id, new DateOnly(2024, 11, 1), 22_000m, 0.19m, 35),
            OpenInv(InvNo(), clientBioNova.Id, new DateOnly(2025, 6, 1), 15_000m, 0.19m),
            OverdueInv(InvNo(), clientBioNova.Id, new DateOnly(2025, 9, 1), 17_000m, 0.19m)
        );
        // Construct Impex (At Risk) — 18k paid, avg +48 days, 45k open
        db.Invoices.AddRange(
            PaidInv(InvNo(), clientConstruct.Id, new DateOnly(2024, 5, 1), 9_000m, 0.19m, 50),
            PaidInv(InvNo(), clientConstruct.Id, new DateOnly(2024, 8, 1), 9_000m, 0.19m, 46),
            OpenInv(InvNo(), clientConstruct.Id, new DateOnly(2025, 3, 1), 15_000m, 0.19m),
            OverdueInv(InvNo(), clientConstruct.Id, new DateOnly(2025, 6, 1), 15_000m, 0.19m),
            OverdueInv(InvNo(), clientConstruct.Id, new DateOnly(2025, 9, 1), 15_000m, 0.19m)
        );
        // OilPro Energy (At Risk) — 15k paid, avg +62 days, 38k open
        db.Invoices.AddRange(
            PaidInv(InvNo(), clientOilPro.Id, new DateOnly(2024, 7, 1), 15_000m, 0.19m, 62),
            OpenInv(InvNo(), clientOilPro.Id, new DateOnly(2025, 4, 1), 18_000m, 0.19m),
            OverdueInv(InvNo(), clientOilPro.Id, new DateOnly(2025, 8, 1), 20_000m, 0.19m)
        );
        await db.SaveChangesAsync(cancellationToken);

        // ── 16. Set UpdatedAt on Done tasks ───────────────────────────────────────
        // SaveChangesAsync leaves UpdatedAt=null for newly added entities.
        // ClusteringService checks t.UpdatedAt >= cutoff (last 90 days).
        // ExecuteUpdateAsync bypasses the override and stamps them directly.
        await db.Tasks
            .Where(t => t.Status == TaskStatus.Done)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UpdatedAt, DateTime.UtcNow), cancellationToken);

        _logger.LogInformation(
            "Database seeded successfully: 15 employees, 10 clients, 30 projects (25 completed + 5 active), " +
            "120 payroll records (8 months, Dec bonus), 25 leave requests (historical + upcoming), " +
            "12 leads, 45+ invoices. Admin: admin@admin.com / admin");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ── Entity factory helpers ────────────────────────────────────────────────

    private static Employee Emp(string first, string last, string workEmail, string dept, string title,
        DateOnly birth, DateOnly hire, float seniority, Guid? managerId = null) => new()
        {
            Id = Guid.NewGuid(),
            FirstName = first,
            LastName = last,
            PersonalEmail = workEmail.Replace("@company.com", "@gmail.com"),
            WorkEmail = workEmail,
            Phone = "+40 721 000 000",
            BirthDate = birth,
            HireDate = hire,
            Department = dept,
            JobTitle = title,
            SeniorityYears = seniority,
            IsActive = true,
            ManagerId = managerId,
        };

    private static EmploymentContract EmpContract(Guid empId, DateOnly start, decimal gross) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = empId,
        ContractType = ContractType.FullTime,
        StartDate = start,
        GrossSalary = gross,
        Currency = "RON",
        IsActive = true,
    };

    private static Client MkClient(string name, string industry, string country, Guid amId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyName = name,
        Industry = industry,
        Country = country,
        AccountManagerId = amId,
        Status = ClientStatus.Active,
    };

    private static ClientContact CC(Guid clientId, string name, string email, string phone, string title) => new()
    {
        Id = Guid.NewGuid(),
        ClientId = clientId,
        FullName = name,
        Email = email,
        Phone = phone,
        JobTitle = title,
        IsPrimary = true,
    };

    private static CrmContract CrmCon(string num, Guid clientId, string title, decimal value, DateOnly start, DateOnly end) => new()
    {
        Id = Guid.NewGuid(),
        ContractNumber = num,
        ClientId = clientId,
        Title = title,
        Value = value,
        Currency = "RON",
        StartDate = start,
        EndDate = end,
        Status = ContractStatus.Signed,
        SignedDate = start.AddDays(-1),
    };

    private static Lead MkLead(string company, string contact, string email, LeadSource source, LeadStage stage,
        decimal value, Guid assignedTo, Guid? convertedId) => new()
        {
            Id = Guid.NewGuid(),
            CompanyName = company,
            ContactName = contact,
            ContactEmail = email,
            Source = source,
            Stage = stage,
            EstimatedValue = value,
            AssignedToUserId = assignedTo,
            ConvertedToClientId = convertedId,
        };

    private static ProjectTask MkTask(Guid projectId, string title, TaskPriority priority,
        TaskStatus status, decimal estimatedHours, Guid createdBy,
        Guid? assignedTo = null, Guid? columnId = null, DateOnly? dueDate = null) => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title,
            Priority = priority,
            Status = status,
            EstimatedHours = estimatedHours,
            CreatedByUserId = createdBy,
            AssignedToEmployeeId = assignedTo,
            KanbanColumnId = columnId,
            DueDate = dueDate,
        };

    private static ProjectMember MkPM(Guid projectId, Guid employeeId, string role, decimal hours, DateOnly join) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        EmployeeId = employeeId,
        Role = role,
        AllocatedHoursPerWeek = hours,
        JoinDate = join,
    };

    private static TimeEntry TE(Guid projectId, Guid taskId, Guid employeeId, DateOnly date, decimal hours, string desc) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        TaskId = taskId,
        EmployeeId = employeeId,
        Date = date,
        HoursLogged = hours,
        Description = desc,
    };

    private static (KanbanColumn Backlog, KanbanColumn InProg, KanbanColumn Review, KanbanColumn Done)
        Cols(Guid boardId) => (
            new KanbanColumn { Id = Guid.NewGuid(), BoardId = boardId, Name = "Backlog", Order = 0 },
            new KanbanColumn { Id = Guid.NewGuid(), BoardId = boardId, Name = "In Progress", Order = 1 },
            new KanbanColumn { Id = Guid.NewGuid(), BoardId = boardId, Name = "In Review", Order = 2 },
            new KanbanColumn { Id = Guid.NewGuid(), BoardId = boardId, Name = "Done", Order = 3 }
        );

    private static Invoice PaidInv(string num, Guid clientId, DateOnly issue, decimal sub, decimal taxRate, int delayDays)
    {
        var due = issue.AddDays(30);
        var tax = Math.Round(sub * taxRate, 2);
        var total = sub + tax;
        return new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            InvoiceNumber = num,
            IssueDate = issue,
            DueDate = due,
            SubTotal = sub,
            TaxRate = taxRate,
            TaxAmount = tax,
            TotalAmount = total,
            PaidAmount = total,
            Status = InvoiceStatus.Paid,
            PaidAt = due.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(delayDays),
        };
    }

    private static Invoice OpenInv(string num, Guid clientId, DateOnly issue, decimal sub, decimal taxRate)
    {
        var due = issue.AddDays(30);
        var tax = Math.Round(sub * taxRate, 2);
        var total = sub + tax;
        return new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            InvoiceNumber = num,
            IssueDate = issue,
            DueDate = due,
            SubTotal = sub,
            TaxRate = taxRate,
            TaxAmount = tax,
            TotalAmount = total,
            PaidAmount = 0m,
            Status = InvoiceStatus.Sent,
        };
    }

    private static Invoice OverdueInv(string num, Guid clientId, DateOnly issue, decimal sub, decimal taxRate)
    {
        var due = issue.AddDays(30);
        var tax = Math.Round(sub * taxRate, 2);
        var total = sub + tax;
        return new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            InvoiceNumber = num,
            IssueDate = issue,
            DueDate = due,
            SubTotal = sub,
            TaxRate = taxRate,
            TaxAmount = tax,
            TotalAmount = total,
            PaidAmount = 0m,
            Status = InvoiceStatus.Overdue,
        };
    }

    private static async Task<ApplicationUser> CreateUser(
        UserManager<ApplicationUser> userManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        string email, string password, string fullName, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Ensure the role is assigned even if the user already exists
            if (!await userManager.IsInRoleAsync(existing, role))
                await userManager.AddToRoleAsync(existing, role);
            return existing;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
