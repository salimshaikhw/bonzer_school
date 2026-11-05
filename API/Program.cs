using API.ExceptionHandling;
using API.Extensions;
using API.Logging;
using API.Middlewares;
using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Application.Common.Helpers.FireAndForget;
using Application.ViewModels.EmailSettings;
using FluentValidation.AspNetCore;
using Infrastructure.Extensions;
using Infrastructure.Messaging;
using Infrastructure.Persistence.Catalog;
using Infrastructure.Persistence.Seed;
using Infrastructure.Tenancy;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Infrastructure.Security.Entities;
using Microsoft.AspNetCore.DataProtection;
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

// Console-only logger until the host is built; replaced by the configured one in AddBonzerLogging.
LoggingExtensions.CreateBootstrapLogger();

// Last-resort logging for exceptions that escape every handler (background threads, forgotten tasks).
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception; the process is terminating: {IsTerminating}", e.IsTerminating);
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    Log.Error(e.Exception, "Unobserved task exception");
    e.SetObserved();
};

try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog: structured JSON logs (console, rolling file, optionally Datadog). See API/Logging.
    builder.AddBonzerLogging();

    // Global exception handling: one log entry and one consistent error response per failed request.
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails(options =>
        // Framework-generated errors (e.g. invalid JSON / model validation) carry the same correlation id.
        options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier);




    // Multi-tenancy (database per school group): CatalogDbContext = shared catalog (tenants, logins,
    // shared lookups); BonzerSchoolDbContext = the current tenant's database. See MultiTenancyExtensions.
    builder.Services.AddMultiTenancy(builder.Configuration);


    // Dev Ops CI CD Check Comment F0r Just Testing 51

    // Identity lives in the catalog database: one login table for every tenant (ApplicationUser.TenantId).
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
        .AddEntityFrameworkStores<CatalogDbContext>()
        .AddDefaultTokenProviders();

    // Keys that protect Identity tokens (password reset, email confirmation links). In containers the
    // default key folder is lost on every restart, so DataProtection:KeysPath points at a volume.
    var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
    if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
    {
        builder.Services.AddDataProtection()
            .SetApplicationName("BonzerSchool")
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
    }

    // Add JWT and token service from Infrastructure.Extensions
    builder.Services.AddInfrastructureJwtAuthentication(builder.Configuration, builder.Environment);

    // After AddInfrastructureJwtAuthentication(...)
    builder.Services.AddHttpContextAccessor();

    // Memory cache
    builder.Services.AddMemoryCache();
    // IMemoryCacheHelper is registered (scoped, tenant-prefixed keys) in AddEntityServices.

    // AutoMapper (adjust assemblies or profiles as needed)
    builder.Services.AddAutoMapper(typeof(Infrastructure.Mapping.BranchProfile).Assembly);
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));



    // Health Checks (only add Npgsql when connection string is set); checks the catalog database.
    var healthBuilder = builder.Services.AddHealthChecks();
    var pgConnection = builder.Configuration.GetConnectionString("PostgresConnection");
    if (!string.IsNullOrEmpty(pgConnection))
        healthBuilder.AddNpgSql(sp => sp.GetRequiredService<TenantConnectionStrings>().Catalog);

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
                    .AllowCredentials()
                    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName);
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
                        .AllowCredentials()
                        .WithExposedHeaders(CorrelationIdMiddleware.HeaderName);
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
                        .AllowCredentials()
                        .WithExposedHeaders(CorrelationIdMiddleware.HeaderName);
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
        options.DocumentFilter<API.Swagger.FlowOrderDocumentFilter>();

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

    var app = builder.Build();

    // Code-first: create/upgrade the catalog database, seed roles, shared lookups and the Super Admin,
    // migrate every tenant database and (when Seed:DemoData is true, i.e. Development) provision the
    // demo tenant with one login per role and a demo school.
    try
    {
        await DatabaseInitializer.InitializeAsync(app.Services, seedDemoData: builder.Configuration.GetValue<bool>("Seed:DemoData"));
    }
    catch (Exception ex)
    {
        Log.Error(ex,
            "Database initialisation failed. Check ConnectionStrings:PostgresConnection, Tenancy:CatalogDatabase and that PostgreSQL is running, then restart the API.");
    }

    // Order matters:
    //   1. correlation id  -> every later log line (including the request summary) carries it
    //   2. request logging -> one summary line per request, with the final status code
    //   3. exception handler -> turns exceptions into error responses, inside the request log
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseBonzerRequestLogging();
    app.UseExceptionHandler();

    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);   // sections collapsed, in flow order
        ui.EnablePersistAuthorization();                                       // keep the Bearer token across refreshes
    });

    // Avoid redirecting local HTTP API traffic to HTTPS (breaks Vite proxy and odd clients).
    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    // CORS must be before Authentication/Authorization
    app.UseCors();

    // IMPORTANT: authentication before authorization
    app.UseAuthentication();
    // Selects the tenant database from the JWT "tenant_id" claim (or X-Tenant for the Super Admin).
    app.UseMiddleware<API.Middlewares.TenantResolutionMiddleware>();
    // Adds TenantId / Tenant / UserId to every log line written while the request runs.
    app.UseMiddleware<RequestLogContextMiddleware>();
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
// EF Core tools (dotnet ef migrations ...) stop the host on purpose with HostAbortedException.
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}