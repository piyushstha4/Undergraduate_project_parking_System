using MySqlConnector;

namespace SmartParking.Api.Infrastructure;

/// <summary>
/// Loads the project .env file and maps its variables onto ASP.NET Core configuration keys.
/// Existing process environment variables win, so a host can still override the file.
/// </summary>
public static class EnvConfig
{
    public static void Apply()
    {
        foreach (var path in FindEnvFiles())
            LoadFile(path);

        ApplyMysqlConnectionString();
        ApplyAliases();
    }

    private static IEnumerable<string> FindEnvFiles()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            for (var i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
            {
                var path = Path.Combine(dir.FullName, ".env");
                if (File.Exists(path) && seen.Add(path))
                    yield return path;
            }
        }
    }

    private static void LoadFile(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;

            var key = line[..eq].Trim();
            var value = Unquote(line[(eq + 1)..].Trim());
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            return value[1..^1];
        return value;
    }

    /// <summary>
    /// MYSQL_* parts are the source of truth. They override the connection string in appsettings.json.
    /// </summary>
    private static void ApplyMysqlConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("MYSQL_HOST");
        var database = Environment.GetEnvironmentVariable("MYSQL_DATABASE");
        var user = Environment.GetEnvironmentVariable("MYSQL_USER");
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(user))
            return;

        var portText = Environment.GetEnvironmentVariable("MYSQL_PORT");
        if (!uint.TryParse(portText, out var port)) port = 3306;

        var builder = new MySqlConnectionStringBuilder
        {
            Server = host,
            Port = port,
            Database = database,
            UserID = user,
            Password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "",
            SslMode = MySqlSslMode.None,
            AllowPublicKeyRetrieval = true,
        };
        Environment.SetEnvironmentVariable("ConnectionStrings__SmartParking", builder.ConnectionString);
    }

    private static void ApplyAliases()
    {
        Copy("JWT_SECRET", "Jwt__Secret");
        Copy("JWT_EXPIRES_IN_DAYS", "Jwt__ExpiresInDays");
        Copy("DB_CREATE_AND_SEED", "Database__CreateAndSeedOnStartup");

        var port = Environment.GetEnvironmentVariable("API_PORT");
        if (!string.IsNullOrWhiteSpace(port) &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://0.0.0.0:{port}");
        }
    }

    private static void Copy(string from, string to)
    {
        var value = Environment.GetEnvironmentVariable(from);
        if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(to)))
            Environment.SetEnvironmentVariable(to, value);
    }
}
