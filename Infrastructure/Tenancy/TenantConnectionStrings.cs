using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Infrastructure.Tenancy;

/// <summary>
/// Builds connection strings. Server, port and credentials come from ConnectionStrings:PostgresConnection
/// (user secrets / environment); only database names are stored in the catalog, so the catalog never
/// holds passwords.
/// </summary>
public sealed class TenantConnectionStrings
{
    private readonly string _server;
    private readonly string? _catalogOverride;
    private readonly TenancyOptions _options;

    public TenantConnectionStrings(IConfiguration configuration, IOptions<TenancyOptions> options)
    {
        _options = options.Value;
        _server = configuration.GetConnectionString("PostgresConnection") ?? string.Empty;
        _catalogOverride = configuration.GetConnectionString("CatalogConnection");
    }

    public string Catalog =>
        !string.IsNullOrWhiteSpace(_catalogOverride) ? _catalogOverride : ForDatabase(_options.CatalogDatabase);

    public string ForDatabase(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(_server))
            throw new InvalidOperationException("ConnectionStrings:PostgresConnection is not configured.");
        return new NpgsqlConnectionStringBuilder(_server) { Database = databaseName }.ConnectionString;
    }

    public string DatabaseNameFor(string identifier) =>
        _options.TenantDatabasePrefix + identifier.Replace('-', '_');
}
