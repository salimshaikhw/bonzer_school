using System;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Infrastructure.Persistence.Catalog;
using Infrastructure.Security.External;
using Infrastructure.Security.OAuth;
using Infrastructure.Security.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;

namespace Infrastructure.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Default scheme of the API. Forwards each request to the handler that can read its bearer token:
    ///   - our own JWT (signed, 3 segments)              -> JwtBearer
    ///   - OAuth 2.0 server access token (encrypted JWE, 5 segments) -> OpenIddict validation
    /// </summary>
    public const string ApiScheme = "ApiBearer";

    /// <summary>
    /// Registers JWT configuration, JwtBearer authentication, the JWT token service, Google/Microsoft
    /// ID-token validation and the OAuth 2.0 server for machine clients.
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

        // Sign in with Google / Microsoft: the SPA sends the provider's ID token, we issue our own JWT.
        services.Configure<ExternalLoginOptions>(configuration.GetSection(ExternalLoginOptions.SectionName));
        services.AddSingleton<IExternalTokenValidator, ExternalTokenValidator>();

        var oauthServerEnabled = services.AddOAuthServer(configuration, hostEnvironment);

        // Configure authentication
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = ApiScheme;
            options.DefaultAuthenticateScheme = ApiScheme;
            options.DefaultChallengeScheme = ApiScheme;
            options.DefaultForbidScheme = ApiScheme;
        })
        .AddPolicyScheme(ApiScheme, "JWT or OAuth 2.0 bearer token", options =>
        {
            options.ForwardDefaultSelector = context =>
                oauthServerEnabled && IsEncryptedToken(context.Request.Headers.Authorization.ToString())
                    ? OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme
                    : JwtBearerDefaults.AuthenticationScheme;
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

    /// <summary>
    /// OAuth 2.0 server (OpenIddict) for machine clients: client_credentials at POST /connect/token.
    /// Client applications are stored in the catalog database (OpenIddict* tables) and are bound to one
    /// tenant and a set of school roles. Token keys come from the "OAuthServer" section; in Development
    /// OpenIddict's development certificates are used when none are configured. Without keys elsewhere
    /// the server stays off and only our own JWTs are accepted.
    /// </summary>
    /// <returns>Whether the token endpoint and OAuth token validation are enabled.</returns>
    private static bool AddOAuthServer(this IServiceCollection services, IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        var options = configuration.GetSection(OAuthServerOptions.SectionName).Get<OAuthServerOptions>() ?? new OAuthServerOptions();
        services.Configure<OAuthServerOptions>(configuration.GetSection(OAuthServerOptions.SectionName));

        // The application store is always registered, so OAuth clients can be managed even while the
        // token endpoint is off.
        var openIddict = services.AddOpenIddict()
            .AddCore(core => core.UseEntityFrameworkCore().UseDbContext<CatalogDbContext>());

        var hasKeys = !string.IsNullOrWhiteSpace(options.SigningCertificate) && !string.IsNullOrWhiteSpace(options.EncryptionKey);
        if (!options.Enabled || (!hasKeys && !hostEnvironment.IsDevelopment()))
            return false;

        openIddict
            .AddServer(server =>
            {
                server.SetTokenEndpointUris(OAuthServerOptions.TokenEndpoint);
                server.AllowClientCredentialsFlow();
                server.RegisterScopes(OAuthServerOptions.ApiScope);
                server.SetAccessTokenLifetime(TimeSpan.FromMinutes(options.AccessTokenLifetimeMinutes));

                if (hasKeys)
                {
                    server.AddSigningCertificate(new X509Certificate2(
                        Convert.FromBase64String(options.SigningCertificate!), options.SigningCertificatePassword,
                        X509KeyStorageFlags.EphemeralKeySet));
                    server.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String(options.EncryptionKey!)));
                }
                else
                {
                    server.AddDevelopmentSigningCertificate();
                    server.AddDevelopmentEncryptionCertificate();
                }

                var aspNetCore = server.UseAspNetCore().EnableTokenEndpointPassthrough();
                if (hostEnvironment.IsDevelopment())
                    aspNetCore.DisableTransportSecurityRequirement();
            })
            .AddValidation(validation =>
            {
                validation.UseLocalServer();
                validation.AddAudiences(OAuthServerOptions.ApiAudience);
                validation.UseAspNetCore();
            });

        return true;
    }

    /// <summary>"Bearer a.b.c.d.e": a compact JWE, as issued by the OAuth server. Our own JWTs have 3 segments.</summary>
    private static bool IsEncryptedToken(string authorization)
    {
        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        var dots = 0;
        for (var i = prefix.Length; i < authorization.Length; i++)
            if (authorization[i] == '.')
                dots++;
        return dots == 4;
    }
}
