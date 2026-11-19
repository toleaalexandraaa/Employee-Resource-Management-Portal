using System.Text;
using company_resource_manager.Server.Data;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Services.Analytics;
using company_resource_manager.Server.Services.Audit;
using company_resource_manager.Server.Services.Auth;
using company_resource_manager.Server.Services.Crm;
using company_resource_manager.Server.Services.Estimation;
using company_resource_manager.Server.Services.Hr;
using company_resource_manager.Server.Services.Interfaces;
using company_resource_manager.Server.Services.Notifications;
using company_resource_manager.Server.Services.Proj;
using company_resource_manager.Server.Services.Seeding;
using company_resource_manager.Server.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Core services ────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

// ── Database ─────────────────────────────────────────────────────────────────
//builder.Services.AddDbContext<AppDbContext>(options =>
//  options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=CompanyResourceManager;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"));

// ── Settings ─────────────────────────────────────────────────────────────────
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<EstimationSettings>(builder.Configuration.GetSection("EstimationSettings"));

// ── Identity ─────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // Allow SignalR hubs to receive the token via query string (?access_token=...)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IEstimationService, EstimationService>();
builder.Services.AddHostedService<EstimationModelStartupService>();
builder.Services.AddHostedService<RoleSeederService>();
builder.Services.AddHostedService<DataSeederService>();

// ── Analytics Services ────────────────────────────────────────────────────────
builder.Services.AddScoped<IOverviewAnalyticsService, OverviewAnalyticsService>();
builder.Services.AddScoped<IHrAnalyticsService, HrAnalyticsService>();
builder.Services.AddScoped<IProjectAnalyticsService, ProjectAnalyticsService>();
builder.Services.AddScoped<ICrmAnalyticsService, CrmAnalyticsService>();
builder.Services.AddScoped<IClusteringService, ClusteringService>();
builder.Services.AddScoped<IRiskScoringService, RiskScoringService>();

// ── HR Services ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmploymentContractService, EmploymentContractService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();

// ── Project Services ──────────────────────────────────────────────────────────
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<IKanbanService, KanbanService>();

// ── CRM Services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ICrmContractService, CrmContractService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ILeadService, LeadService>();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapFallbackToFile("/index.html");

app.Run();
