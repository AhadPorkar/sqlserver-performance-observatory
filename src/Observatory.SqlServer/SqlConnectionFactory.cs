using Microsoft.Data.SqlClient;
namespace Observatory.SqlServer;
public sealed class SqlConnectionFactory(CollectionOptions options)
{
    public SqlConnection Create(TargetOptions target)
    {
        var value = Environment.GetEnvironmentVariable(target.ConnectionEnvironment);
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("CONNECTION_SECRET_MISSING");
        var b = new SqlConnectionStringBuilder(value);
        if (string.IsNullOrWhiteSpace(b.InitialCatalog) || new[] { "master", "tempdb", "model", "msdb" }.Contains(b.InitialCatalog, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("EXPLICIT_USER_DATABASE_REQUIRED");
        if (b.UserID.Equals("sa", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SYSADMIN_USERNAME_REJECTED");
        if (b.TrustServerCertificate && !(options.AllowUntrustedCertificateForLocalLab && IsLocalLab(b)))
            throw new InvalidOperationException("CERTIFICATE_VALIDATION_REQUIRED");
        // ReadOnly is a routing hint, NOT a security boundary. SQL grants enforce rights.
        b.ApplicationIntent = ApplicationIntent.ReadOnly;
        b.ApplicationName = "SQL Performance Observatory/0.1";
        b.Encrypt = SqlConnectionEncryptOption.Mandatory;
        b.ConnectTimeout = options.CommandTimeoutSeconds;
        b.ConnectRetryCount = 0;
        b.MaxPoolSize = 4;
        b.MinPoolSize = 0;
        b.MultipleActiveResultSets = false;
        b.PersistSecurityInfo = false;
        return new SqlConnection(b.ConnectionString);
    }
    public static bool IsLocalLab(SqlConnectionStringBuilder b)
    {
        var server = b.DataSource.Replace("tcp:", "", StringComparison.OrdinalIgnoreCase).Split(',')[0];
        return b.InitialCatalog == "ObservatoryLab" && (server is "localhost" or "127.0.0.1" or "::1" or "sql-lab");
    }
}
