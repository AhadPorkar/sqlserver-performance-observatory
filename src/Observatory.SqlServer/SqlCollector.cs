using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using Microsoft.Data.SqlClient;
using Observatory.Core;
namespace Observatory.SqlServer;

public sealed record QueryCollection(QueryStoreInfo Info, QueryBucket[] Buckets, bool Truncated);
public sealed record BoundedResult<T>(T[] Items, bool Truncated);

public sealed class SqlCollector(CollectionOptions options)
{
    private readonly SqlConnectionFactory connections = new(options);
    public async Task<QueryCollection> ReadQueriesAsync(TargetOptions target, CancellationToken ct)
    {
        await using var connection = connections.Create(target);
        await connection.OpenAsync(ct);
        QueryStoreInfo info;
        using (var cmd = Command(connection, "Capabilities"))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) throw new InvalidDataException("NO_METADATA");
            int major = r.GetInt32(0), db = r.GetInt32(1);
            var time = r.GetFieldValue<DateTimeOffset>(2);
            if (major < 15) throw new InvalidOperationException("SQL_SERVER_2019_OR_LATER_REQUIRED");
            if (!await r.NextResultAsync(ct) || !await r.ReadAsync(ct)) throw new InvalidDataException("QUERY_STORE_METADATA_UNAVAILABLE");
            info = new(r.GetString(0), r.GetString(1), Convert.ToInt32(r.GetValue(2), CultureInfo.InvariantCulture),
                Convert.ToInt64(r.GetValue(3), CultureInfo.InvariantCulture), Convert.ToInt64(r.GetValue(4), CultureInfo.InvariantCulture),
                Convert.ToInt64(r.GetValue(5), CultureInfo.InvariantCulture), db, major, time,
                r.IsDBNull(6) ? null : r.GetFieldValue<DateTimeOffset>(6));
        }
        if (info.State is "OFF" or "ERROR") return new(info, [], false);
        using var query = Command(connection, "QueryBuckets");
        query.Parameters.Add("@Take", SqlDbType.Int).Value = options.QueryRowLimit + 1;
        query.Parameters.Add("@FromUtc", SqlDbType.DateTimeOffset).Value = (info.LatestClosedEndUtc ?? info.ServerUtc).AddHours(-options.LookbackHours);
        query.Parameters.Add("@ToUtc", SqlDbType.DateTimeOffset).Value = info.ServerUtc;
        var buckets = new List<QueryBucket>();
        await using var rows = await query.ExecuteReaderAsync(ct);
        while (await rows.ReadAsync(ct))
        {
            buckets.Add(new(rows.GetInt64(0), rows.GetInt64(1), rows.GetString(2), rows.GetInt64(3), rows.GetString(4),
                rows.GetBoolean(5), rows.GetFieldValue<DateTimeOffset>(6), rows.GetFieldValue<DateTimeOffset>(7), rows.GetInt64(8),
                Convert.ToDouble(rows.GetValue(9), CultureInfo.InvariantCulture), Convert.ToDouble(rows.GetValue(10), CultureInfo.InvariantCulture),
                Convert.ToDouble(rows.GetValue(11), CultureInfo.InvariantCulture), Convert.ToDouble(rows.GetValue(12), CultureInfo.InvariantCulture)));
        }
        if (buckets.Any(b => !AnalysisEngine.Valid(b))) throw new InvalidDataException("INVALID_QUERY_COUNTERS");
        return new(info, buckets.Take(options.QueryRowLimit).ToArray(), buckets.Count > options.QueryRowLimit);
    }
    public async Task<BoundedResult<BlockingRow>> ReadBlockingAsync(TargetOptions target, CancellationToken ct)
    {
        await using var connection = connections.Create(target); await connection.OpenAsync(ct);
        await RequireServerDiagnosticsAsync(connection, ct);
        using var cmd = Command(connection, "Blocking");
        cmd.Parameters.Add("@Take", SqlDbType.Int).Value = options.BlockingRowLimit + 1;
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var items = new List<BlockingRow>();
        while (await r.ReadAsync(ct))
            items.Add(new(Convert.ToInt32(r.GetValue(0), CultureInfo.InvariantCulture), Convert.ToInt32(r.GetValue(1), CultureInfo.InvariantCulture),
                r.GetString(2), r.GetString(3), r.GetInt64(4), r.GetInt64(5), Convert.ToInt32(r.GetValue(6), CultureInfo.InvariantCulture), r.GetString(7),
                r.IsDBNull(8) ? null : Convert.ToInt32(r.GetValue(8), CultureInfo.InvariantCulture),
                r.IsDBNull(9) ? null : Convert.ToInt32(r.GetValue(9), CultureInfo.InvariantCulture)));
        return new(items.Take(options.BlockingRowLimit).ToArray(), items.Count > options.BlockingRowLimit);
    }
    public async Task<BoundedResult<DeadlockEvent>> ReadDeadlocksAsync(TargetOptions target, int databaseId, CancellationToken ct)
    {
        await using var connection = connections.Create(target); await connection.OpenAsync(ct);
        await RequireServerDiagnosticsAsync(connection, ct);
        using var cmd = Command(connection, "Deadlocks");
        cmd.Parameters.Add("@Take", SqlDbType.Int).Value = options.DeadlockLimit + 1;
        cmd.Parameters.Add("@Pattern", SqlDbType.NVarChar, 2048).Value = target.XeFilePattern;
        cmd.Parameters.Add("@LookbackHours", SqlDbType.Int).Value = options.LookbackHours;
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var items = new List<DeadlockEvent>(); int read = 0;
        while (await r.ReadAsync(ct))
        {
            read++;
            var timestamp = new DateTimeOffset(DateTime.SpecifyKind(r.GetDateTime(0), DateTimeKind.Utc));
            if (r.GetChars(1, 0, null, 0, 0) > DeadlockParser.MaxXmlCharacters) throw new InvalidDataException("XE_XML_TOO_LARGE");
            var xml = r.GetString(1);
            var parsed = DeadlockParser.Parse(xml, timestamp, databaseId);
            if (parsed is not null) items.Add(parsed);
        }
        // If the server-wide cap is hit, another database may have hidden this
        // target's events. The status remains truncated even after filtering.
        return new(items.DistinctBy(x => x.Id).Take(options.DeadlockLimit).ToArray(), read > options.DeadlockLimit);
    }
    private async Task RequireServerDiagnosticsAsync(SqlConnection connection, CancellationToken ct)
    {
        using var check = new SqlCommand("SELECT CASE WHEN CAST(SERVERPROPERTY('ProductMajorVersion') AS int)>=16 THEN HAS_PERMS_BY_NAME(NULL,'SERVER','VIEW SERVER PERFORMANCE STATE') ELSE HAS_PERMS_BY_NAME(NULL,'SERVER','VIEW SERVER STATE') END;", connection)
            { CommandTimeout = options.CommandTimeoutSeconds };
        if (Convert.ToInt32(await check.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture) != 1)
            throw new UnauthorizedAccessException("PERMISSION_DENIED");
    }
    private SqlCommand Command(SqlConnection connection, string query)
        => new("SET DEADLOCK_PRIORITY LOW; SET LOCK_TIMEOUT 1000; SET NOCOUNT ON; " + SqlQueries.Load(query), connection)
        { CommandTimeout = options.CommandTimeoutSeconds };

    public static string ErrorCode(Exception error) => error switch
    {
        SqlException { Number: -2 or 1222 } => "TIMEOUT",
        SqlException { Number: 229 or 297 or 300 } => "PERMISSION_DENIED",
        SqlException { Number: 18456 or 4060 } => "CONNECTION_DENIED",
        SqlException { Number: 40538 } => "UNSUPPORTED_XE_LOCATION",
        UnauthorizedAccessException => "PERMISSION_DENIED",
        XmlException or InvalidDataException => "INVALID_SOURCE_DATA",
        InvalidOperationException or ArgumentException => "CONFIGURATION_OR_CAPABILITY_ERROR",
        _ => "SOURCE_UNAVAILABLE"
    };
}
