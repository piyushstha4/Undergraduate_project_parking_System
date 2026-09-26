using Dapper;
using MySqlConnector;

namespace SmartParking.Api.Data;

/// <summary>
/// Creates the database + tables if they don't exist and seeds demo data into an empty
/// database (the C# port of backend/src/db/seed.js). Controlled by
/// "Database:CreateAndSeedOnStartup" in appsettings.json.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<Db>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        // 1. Create the database itself (connect without selecting a database first).
        var csb = new MySqlConnectionStringBuilder(db.ConnectionString);
        var dbName = csb.Database;
        if (string.IsNullOrWhiteSpace(dbName))
            throw new InvalidOperationException("The connection string must include Database=...");
        csb.Database = "";
        await using (var serverConn = new MySqlConnection(csb.ConnectionString))
        {
            await serverConn.OpenAsync();
            await serverConn.ExecuteAsync(
                $"CREATE DATABASE IF NOT EXISTS `{dbName.Replace("`", "")}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci");
        }

        // 2. Create tables. Run each statement on its own so a comment or
        //    a later statement cannot hide an earlier failure.
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Data", "schema.sql");
        var schema = await File.ReadAllTextAsync(schemaPath);
        await using var conn = await db.OpenAsync();
        foreach (var statement in SplitSql(schema))
            await conn.ExecuteAsync(statement);

        var hasCapacity = await conn.ExecuteScalarAsync<long>(
            @"SELECT COUNT(*) FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'parking_areas' AND COLUMN_NAME = 'capacity'");
        if (hasCapacity == 0)
            await conn.ExecuteAsync("ALTER TABLE parking_areas ADD COLUMN capacity INT NOT NULL DEFAULT 1 AFTER price_per_hour");

        // 3. The only seeded account is the administrator. Drivers register themselves.
        //    Parking owners are created later by the admin, who gives them their login.
        await EnsureAdminAsync(conn, logger);
        await RemoveLegacyDemoAccountsAsync(conn, logger);
    }

    public const string AdminEmail = "admin@gmail.com";
    public const string AdminPassword = "admin@123";

    private static async Task EnsureAdminAsync(System.Data.Common.DbConnection conn, ILogger logger)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(AdminPassword, 10);
        var adminId = await conn.ExecuteScalarAsync<int?>(
            "SELECT id FROM users WHERE role = 'admin' ORDER BY id LIMIT 1");

        if (adminId == null)
        {
            var taken = await conn.ExecuteScalarAsync<int?>(
                "SELECT id FROM users WHERE email = @email", new { email = AdminEmail });
            if (taken != null)
                throw new InvalidOperationException($"Cannot create the admin account because {AdminEmail} is already registered.");

            await conn.ExecuteAsync(
                @"INSERT INTO users (name, email, phone, password_hash, role)
                  VALUES ('System Admin', @email, '9800000000', @hash, 'admin')",
                new { email = AdminEmail, hash });
            logger.LogInformation("Created administrator {Email}.", AdminEmail);
            return;
        }

        await conn.ExecuteAsync(
            "UPDATE users SET name = 'System Admin', email = @email, password_hash = @hash, status = 'active' WHERE id = @id",
            new { id = adminId, email = AdminEmail, hash });
        logger.LogInformation("Administrator login is {Email}.", AdminEmail);
    }

    /// <summary>
    /// Removes the old seeded driver and owner logins. Owners are created by the admin;
    /// drivers register themselves. Parking areas owned by those accounts are removed with them.
    /// </summary>
    private static async Task RemoveLegacyDemoAccountsAsync(System.Data.Common.DbConnection conn, ILogger logger)
    {
        var removed = await conn.ExecuteAsync(
            @"DELETE FROM users WHERE email IN (
                'admin@smartparking.com',
                'owner1@smartparking.com',
                'owner2@smartparking.com',
                'user@smartparking.com',
                'anisha@smartparking.com')");
        if (removed > 0)
            logger.LogInformation("Removed {Count} legacy demo account(s).", removed);
    }

    private static IEnumerable<string> SplitSql(string script)
    {
        var parts = script.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var statement = string.Join(
                '\n',
                part.Split('\n').Where(line => !line.TrimStart().StartsWith("--")));
            if (!string.IsNullOrWhiteSpace(statement))
                yield return statement;
        }
    }
}
