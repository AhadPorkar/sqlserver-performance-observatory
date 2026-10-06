using Microsoft.Data.SqlClient;
using Observatory.SqlServer;
namespace Observatory.Lab;
public sealed class LabEnvironment
{
    private readonly SqlConnectionStringBuilder settings;
    public LabEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable("SPO_LAB_CONNECTION")
            ?? throw new InvalidOperationException("Set SPO_LAB_CONNECTION for a dedicated local Developer instance.");
        settings = new(raw);
        if (!SqlConnectionFactory.IsLocalLab(settings)) throw new InvalidOperationException("Lab permits only local host aliases and Database=ObservatoryLab.");
        settings.ApplicationName = "Observatory isolated lab";
        settings.ConnectTimeout = 10; settings.Encrypt = SqlConnectionEncryptOption.Mandatory;
        settings.MultipleActiveResultSets = false;
    }
    public async Task<SqlConnection> OpenAsync(bool master, CancellationToken ct)
    {
        var b = new SqlConnectionStringBuilder(settings.ConnectionString);
        if (master) b.InitialCatalog = "master";
        var connection = new SqlConnection(b.ConnectionString);
        try
        {
            await connection.OpenAsync(ct);
            using var edition = new SqlCommand("SELECT CAST(SERVERPROPERTY('Edition') AS nvarchar(128));", connection);
            var name = Convert.ToString(await edition.ExecuteScalarAsync(ct));
            if (name is null || !name.Contains("Developer", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Lab mutations require SQL Server Developer edition; production editions are refused.");
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }
    public static async Task CheckMarkerAsync(SqlConnection connection, CancellationToken ct)
    {
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.ObservatoryLabMarker WHERE Id=1 AND Project=N'sqlserver-performance-observatory';", connection);
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) != 1) throw new InvalidOperationException("Lab marker missing. Refusing mutation.");
    }
    public static string Script(string name)
    {
        using var stream = typeof(LabEnvironment).Assembly.GetManifestResourceStream($"Observatory.Lab.Sql.{name}.sql")
            ?? throw new InvalidOperationException("Lab resource missing.");
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
    public static async Task ExecuteAsync(SqlConnection c, string sql, CancellationToken ct)
    {
        using var cmd = new SqlCommand(sql, c) { CommandTimeout = 120 };
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task SetupAsync(CancellationToken ct)
    {
        await using var master = await OpenAsync(true, ct);
        using var check = new SqlCommand("SELECT DB_ID(N'ObservatoryLab');", master);
        bool existed = (await check.ExecuteScalarAsync(ct)) is not DBNull and not null;
        if (!existed) await ExecuteAsync(master, "CREATE DATABASE [ObservatoryLab];", ct);
        await using var db = await OpenAsync(false, ct);
        // An existing database without our marker is never adopted or reset.
        if (existed) await CheckMarkerAsync(db, ct);
        await ExecuteAsync(db, Script("Schema"), ct);
        await CheckMarkerAsync(db, ct);
        await ExecuteAsync(db, Script("Procedure"), ct);
        await ExecuteAsync(db, Script("QueryStore"), ct);
        var password = Environment.GetEnvironmentVariable("SPO_LAB_READER_PASSWORD");
        if (password is null || password.Length < 24 || password.Length > 128)
            throw new InvalidOperationException("Set a random SPO_LAB_READER_PASSWORD of 24..128 characters.");
        using var login = new SqlCommand("""
            IF SUSER_ID(N'spo_lab_reader') IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE LOGIN [spo_lab_reader] WITH PASSWORD = N' + NCHAR(39) + REPLACE(@Password, NCHAR(39), NCHAR(39)+NCHAR(39)) + NCHAR(39) + N', CHECK_POLICY = ON;';
                EXEC sys.sp_executesql @sql;
            END;
            """, master);
        login.Parameters.Add("@Password", System.Data.SqlDbType.NVarChar, 128).Value = password;
        await login.ExecuteNonQueryAsync(ct);
        await ExecuteAsync(db, "IF USER_ID(N'spo_lab_reader') IS NULL CREATE USER [spo_lab_reader] FOR LOGIN [spo_lab_reader]; GRANT CONNECT TO [spo_lab_reader];", ct);
        using var version = new SqlCommand("SELECT CAST(SERVERPROPERTY('ProductMajorVersion') AS int);", db);
        int major = Convert.ToInt32(await version.ExecuteScalarAsync(ct));
        await ExecuteAsync(db, major >= 16 ? "GRANT VIEW DATABASE PERFORMANCE STATE TO [spo_lab_reader];" : "GRANT VIEW DATABASE STATE TO [spo_lab_reader];", ct);
        await ExecuteAsync(master, major >= 16 ? "GRANT VIEW SERVER PERFORMANCE STATE TO [spo_lab_reader];" : "GRANT VIEW SERVER STATE TO [spo_lab_reader];", ct);
        Console.WriteLine("Lab schema and reader grants prepared. Existing login passwords are NOT rotated.");
    }
}
