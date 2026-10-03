using System.Text.Json;
using Npgsql;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var appsettings = Path.Combine(repoRoot, "API", "appsettings.json");

if (!File.Exists(appsettings))
{
    Console.Error.WriteLine($"Missing: {appsettings}");
    return 1;
}

string cs;
try
{
    using var doc = JsonDocument.Parse(File.ReadAllText(appsettings));
    var pg = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("PostgresConnection").GetString();
    if (string.IsNullOrWhiteSpace(pg))
    {
        Console.Error.WriteLine("PostgresConnection missing in appsettings.");
        return 1;
    }
    cs = pg;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Invalid appsettings: {ex.Message}");
    return 1;
}

if (args.Length > 0
    && (args[0].Equals("schema", StringComparison.OrdinalIgnoreCase)
        || args[0].Equals("patch", StringComparison.OrdinalIgnoreCase)))
{
    var schemaFiles = new[]
    {
        "add_school_holidays_branch_weekly_off.sql",
    };
    await using var conn = new NpgsqlConnection(cs);
    await conn.OpenAsync();
    foreach (var name in schemaFiles)
    {
        var path = Path.Combine(repoRoot, "Scripts", name);
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Missing: {path}");
            return 1;
        }
        var sql = await File.ReadAllTextAsync(path);
        await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 600 };
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine($"OK: {name}");
    }
    return 0;
}

var sqlPath = Path.Combine(repoRoot, "Scripts", "seed_admission_attendance_scenario.sql");
if (!File.Exists(sqlPath))
{
    Console.Error.WriteLine($"Missing: {sqlPath}");
    return 1;
}

var seedSql = await File.ReadAllTextAsync(sqlPath);
await using var seedConn = new NpgsqlConnection(cs);
await seedConn.OpenAsync();
await using var seedCmd = new NpgsqlCommand(seedSql, seedConn) { CommandTimeout = 600 };
await seedCmd.ExecuteNonQueryAsync();
Console.WriteLine("OK: seed_admission_attendance_scenario.sql applied.");
return 0;
