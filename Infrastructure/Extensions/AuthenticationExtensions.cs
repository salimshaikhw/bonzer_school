using System;
using System.Security.Claims;
using System.Text;
using Infrastructure.Security.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Registers JWT configuration, JwtBearer authentication and the JWT token service.
    /// Call from Program.cs: <c>AddInfrastructureJwtAuthentication(builder.Configuration, builder.Environment)</c>.
    /// </summary>
    public static IServiceCollection AddInfrastructureJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        var jwtSection = configuration.GetSection("Jwt");

        // Bind configuration POCO
        services.Configure<JWTConfiguration>(jwtSection);

        // Ensure key is present
        var key = jwtSection.GetValue<string>("Key") ?? throw new InvalidOperationException("Jwt:Key is required");

        // Register token service implementation
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Configure authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Local API runs on http://localhost — metadata over HTTPS is not available/needed.
            options.RequireHttpsMetadata = !hostEnvironment.IsDevelopment();
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = true,
                ValidIssuer = jwtSection.GetValue<string>("Issuer"),
                ValidateAudience = true,
                ValidAudience = jwtSection.GetValue<string>("Audience"),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role
            };
        });

        return services;
    }
}