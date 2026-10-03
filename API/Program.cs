using API.Extensions;
using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Application.Common.Helpers.FireAndForget;
using Application.ViewModels.EmailSettings;
using FluentValidation.AspNetCore;
using Infrastructure.Extensions;
using Infrastructure.Messaging;
using Infrastructure.Persistence.Postgres;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Infrastructure.Security.Entities;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug)
    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "Logs", "nxt_logs_.txt"),
                  rollingInterval: RollingInterval.Day,
                  restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog as the host logger
    builder.Host.UseSerilog();




    builder.Services.AddDbContext<BonzerSchoolDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));


    // Dev Ops CI CD Check Comment F0r Just Testing 51

    // Identity (use ApplicationDbContext, not BonzerSchoolDbContext)
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
    })
        .AddEntityFrameworkStores<BonzerSchoolDbContext>()
        .AddDefaultTokenProviders();

    // Add JWT and token service from Infrastructure.Extensions
    builder.Services.AddInfrastructureJwtAuthentication(builder.Configuration, builder.Environment);

    // After AddInfrastructureJwtAuthentication(...)
    builder.Services.AddHttpContextAccessor();

    // Memory cache
    builder.Services.AddMemoryCache();
    // Register project-specific memory cache helper if implemented
    builder.Services.AddSingleton<Application.Common.Helpers.MemoryCache.IMemoryCacheHelper, Application.Common.Helpers.MemoryCache.MemoryCacheHelper>();

    // AutoMapper (adjust assemblies or profiles as needed)
    builder.Services.AddAutoMapper(typeof(Infrastructure.Mapping.SchoolGroupProfile).Assembly);
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));



    // Health Checks (only add Npgsql when connection string is set)
    var healthBuilder = builder.Services.AddHealthChecks();
    var pgConnection = builder.Configuration.GetConnectionString("PostgresConnection");
    if (!string.IsNullOrEmpty(pgConnection))
        healthBuilder.AddNpgSql(pgConnection);

    // For Time Conversion 
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    // Register IUnitOfWork with its implementation
    builder.Services.AddScoped<Application.Abstractions.IUnitOfWork, Infrastructure.Persistence.UnitOfWork.UnitOfWork>();

    builder.Services.AddEntityServices();

    // CORS: allow React / Vite dev (any localhost port — Vite may use 3000–3010+ if busy)
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy
                    .SetIsOriginAllowed(static origin =>
                    {
                        if (string.IsNullOrEmpty(origin)) return false;
                        try
                        {
                            var uri = new Uri(origin);
                            return uri.Scheme == "http" && (uri.Host == "localhost" || uri.Host == "127.0.0.1");
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
            else
            {
                // Production / hosted: set Cors:AllowedOrigins in appsettings or environment (SaaS-style split UI + API).
                var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?.Where(static o => !string.IsNullOrWhiteSpace(o))
                    .Select(static o => o.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (origins is { Length: > 0 })
                {
                    policy.WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    policy.WithOrigins(
                            "http://localhost:3000",
                            "http://localhost:3001",
                            "http://localhost:5173"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            }
        });
    });

    // SPA sends camelCase (email, password). System.Text.Json is case-sensitive by default — bind without forcing C# property casing.
    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();
    FluentValidation.ServiceCollectionExtensions.AddValidatorsFromAssemblyContaining(builder.Services, typeof(Application.Validation.Payment.PaymentRequestValidator));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        var xmlFile = $"API.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        // Add JWT Bearer support
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: 'Authorization: Bearer {token}'",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer"
        });
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
    });

    // Configure logging providers and filters
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();
    builder.Logging.AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning);
    builder.Logging.AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", Microsoft.Extensions.Logging.LogLevel.Warning);

    var app = builder.Build();

    // Seed Identity roles and demo users (AspNetUsers / AspNetUserRoles). Legacy app "users" table is not filled here.
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = new[] { "Admin", "User", "Principal", "ClassTeacher", "Parent" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            async Task EnsureIdentityUserAsync(string email, string password, string roleName, string firstName, string lastName)
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return;

                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FirstName = firstName,
                        LastName = lastName,
                    };
                    var created = await userManager.CreateAsync(user, password);
                    if (!created.Succeeded)
                    {
                        Log.Warning(
                            "Identity seed: could not create {Email}: {Errors}",
                            email,
                            string.Join("; ", created.Errors.Select(e => e.Description)));
                        return;
                    }
                }

                if (await userManager.IsInRoleAsync(user, roleName))
                    return;

                var addRole = await userManager.AddToRoleAsync(user, roleName);
                if (!addRole.Succeeded)
                {
                    Log.Warning(
                        "Identity seed: could not add role {Role} to {Email}: {Errors}",
                        roleName,
                        email,
                        string.Join("; ", addRole.Errors.Select(e => e.Description)));
                }
            }

            var adminEmail = builder.Configuration["Admin:Email"] ?? "admin@local";
            var adminPwd = builder.Configuration["Admin:Password"] ?? "Admin123!";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, FirstName = "Sys", LastName = "Admin" };
                var res = await userManager.CreateAsync(admin, adminPwd);
                if (res.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            var demoSection = builder.Configuration.GetSection("DemoUsers");
            var demoPwd = demoSection["Password"];
            if (!string.IsNullOrWhiteSpace(demoPwd))
            {
                await EnsureIdentityUserAsync(
                    demoSection["PrincipalEmail"] ?? "principal@local",
                    demoPwd,
                    "Principal",
                    "Scenario",
                    "Principal");
                await EnsureIdentityUserAsync(
                    demoSection["ClassTeacherEmail"] ?? "classteacher@local",
                    demoPwd,
                    "ClassTeacher",
                    "Scenario",
                    "ClassTeacher");
                await EnsureIdentityUserAsync(
                    demoSection["ParentEmail"] ?? "parent@local",
                    demoPwd,
                    "Parent",
                    "Scenario",
                    "Parent");
                await EnsureIdentityUserAsync(
                    demoSection["StaffEmail"] ?? "staff@local",
                    demoPwd,
                    "User",
                    "Scenario",
                    "Staff");
            }
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex,
            "Identity seed skipped (database not reachable). Start PostgreSQL on the connection in appsettings.json, then restart the API for login and data endpoints.");
    }


        app.UseSwagger();
        app.UseSwaggerUI();
    


    // Global exception handling
    app.UseMiddleware<API.Middlewares.ExceptionMiddleware>();
    // Avoid redirecting local HTTP API traffic to HTTPS (breaks Vite proxy and odd clients).
    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    // CORS must be before Authentication/Authorization
    app.UseCors();

    // IMPORTANT: authentication before authorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    // Health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/sql", new HealthCheckOptions
    {
        Predicate = check => check.Name == "SQL Server"
    });
    app.MapHealthChecks("/health/pgsql", new HealthCheckOptions
    {
        Predicate = check => check.Name == "PostgreSQL"
    });
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}