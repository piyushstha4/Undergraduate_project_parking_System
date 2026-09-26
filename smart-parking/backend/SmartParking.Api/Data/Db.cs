using Dapper;
using MySqlConnector;

namespace SmartParking.Api.Data;

/// <summary>
/// Opens MySQL connections. Every connection's session time zone is set to UTC so that
/// CURRENT_TIMESTAMP defaults and the DATETIME values we write are all UTC.
/// </summary>
public class Db
{
    private readonly string _connectionString;

    public Db(IConfiguration config)
    {
        var raw = config.GetConnectionString("SmartParking")
            ?? throw new InvalidOperationException("Connection string 'SmartParking' is missing in appsettings.json.");

        // DateTimeKind=Utc => DATETIME columns are read back as UTC, so the JSON
        // output ends with 'Z' and the React app converts it to local time correctly.
        var builder = new MySqlConnectionStringBuilder(raw) { DateTimeKind = MySqlDateTimeKind.Utc };
        _connectionString = builder.ConnectionString;
    }

    public string ConnectionString => _connectionString;

    public async Task<MySqlConnection> OpenAsync()
    {
        var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync("SET time_zone = '+00:00'");
        return conn;
    }
}
