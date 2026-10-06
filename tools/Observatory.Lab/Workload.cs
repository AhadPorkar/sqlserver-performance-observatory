using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Observatory.Core;
using Observatory.SqlServer;
namespace Observatory.Lab;
public sealed record WorkloadSample(int CustomerId, double ClientElapsedMs, long? LogicalReads, decimal Result);
public sealed record WorkloadPhase(string Name, DateTimeOffset StartedUtc, DateTimeOffset EndedUtc,
    WorkloadSample[] Samples, double MedianClientMs, double P95ClientMs, double? MeanLogicalReads);
public sealed partial class Workload(LabEnvironment lab)
{
    private static readonly int[] Parameters = [17, 29, 501, 901, 1337];
    public async Task<object> BenchmarkAsync(bool alignIntervals, CancellationToken ct)
    {
        await using var db = await lab.OpenAsync(false, ct); await LabEnvironment.CheckMarkerAsync(db, ct);
        await LabEnvironment.ExecuteAsync(db, "SET LANGUAGE us_english; SET STATISTICS IO ON;", ct);
        using var sizeCommand = new SqlCommand("SELECT COUNT_BIG(*) FROM dbo.Orders;", db);
        if (Convert.ToInt64(await sizeCommand.ExecuteScalarAsync(ct)) != 100000)
            throw new InvalidDataException("The controlled seed must contain exactly 100000 rows.");
        var phases = new List<WorkloadPhase>();
        await EnsureIndexAsync(db, ct);
        try
        {
            phases.Add(await PhaseAsync(db, "baseline-index-present", alignIntervals, ct));
            await LabEnvironment.ExecuteAsync(db, "DROP INDEX IX_Orders_CustomerId ON dbo.Orders;", ct);
            phases.Add(await PhaseAsync(db, "regression-index-removed", alignIntervals, ct));
            await EnsureIndexAsync(db, ct);
            phases.Add(await PhaseAsync(db, "repaired-index-restored", alignIntervals, ct));
        }
        finally { await EnsureIndexAsync(db, CancellationToken.None); }
        bool sameResults = phases.Skip(1).All(p => p.Samples.Select(s => s.Result).SequenceEqual(phases[0].Samples.Select(s => s.Result)));
        if (!sameResults) throw new InvalidDataException("The result sequence changed; comparison is invalid.");
        await LabEnvironment.ExecuteAsync(db, "EXEC sys.sp_query_store_flush_db;", ct); // LAB ONLY.
        using var versionCommand = new SqlCommand("SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128));", db);
        var sqlVersion = Convert.ToString(await versionCommand.ExecuteScalarAsync(ct));
        var collector = new SqlCollector(new CollectionOptions { AllowUntrustedCertificateForLocalLab = true });
        var queryStore = await collector.ReadQueriesAsync(new TargetOptions { Id = "lab", ConnectionEnvironment = "SPO_LAB_READER_CONNECTION" }, ct);
        return new { schemaVersion = 1, kind = "measured-local-lab", sqlVersion, queryStore,
            queryStoreAttribution = "Buckets may include warmups and other lab commands. Use timestamps and query IDs; do not equate bucket counts with client measured samples.", generatedUtc = DateTimeOffset.UtcNow,
            queryStoreIntervalsSeparated = alignIntervals, seedRows = 100000, measuredExecutionsPerPhase = 30,
            warmupsPerPhase = 5, identicalParametersAndResults = sameResults,
            noGlobalCacheFlush = true, phases,
            interpretation = "Local measurements only. Client latency includes network/driver cost. Logical reads come from STATISTICS IO. No production or guaranteed improvement claim." };
    }
    private static async Task EnsureIndexAsync(SqlConnection db, CancellationToken ct)
        => await LabEnvironment.ExecuteAsync(db, "IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Orders') AND name=N'IX_Orders_CustomerId') CREATE INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId) INCLUDE(Total);", ct);
    private static async Task<WorkloadPhase> PhaseAsync(SqlConnection db, string name, bool align, CancellationToken ct)
    {
        // Warm each phase identically, including compilation, without global cache clears.
        foreach (int p in Parameters) await RunAsync(db, p, ct);
        if (align) await WaitBoundaryAsync(db, ct);
        var start = DateTimeOffset.UtcNow;
        var samples = new List<WorkloadSample>();
        for (int i = 0; i < 30; i++) samples.Add(await RunAsync(db, Parameters[i % Parameters.Length], ct));
        var end = DateTimeOffset.UtcNow;
        if (samples.Any(s => !s.LogicalReads.HasValue))
            throw new InvalidDataException("STATISTICS IO for Orders was not observed for every measured sample.");
        if (align) await WaitBoundaryAsync(db, ct);
        await LabEnvironment.ExecuteAsync(db, "EXEC sys.sp_query_store_flush_db;", ct);
        return new(name, start, end, samples.ToArray(), Percentiles.NearestRank(samples.Select(s => s.ClientElapsedMs), .5),
            Percentiles.NearestRank(samples.Select(s => s.ClientElapsedMs), .95),
            samples.All(x => x.LogicalReads.HasValue) ? samples.Average(x => (double)x.LogicalReads!.Value) : null);
    }
    private static async Task<WorkloadSample> RunAsync(SqlConnection db, int customer, CancellationToken ct)
    {
        long reads = 0; bool observed = false;
        SqlInfoMessageEventHandler handler = (_, e) =>
        {
            foreach (SqlError message in e.Errors)
            {
                // Ignore Worktable statistics; isolate the seeded Orders table.
                if (!message.Message.Contains("Table 'Orders'", StringComparison.Ordinal)) continue;
                var match = ReadsPattern().Match(message.Message);
                if (match.Success) { reads += long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture); observed = true; }
            }
        };
        db.InfoMessage += handler;
        try
        {
            using var cmd = new SqlCommand("dbo.GetCustomerTotal", db) { CommandType = CommandType.StoredProcedure, CommandTimeout = 30 };
            cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customer;
            var watch = Stopwatch.StartNew();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new InvalidDataException("Workload returned no row.");
            var result = reader.GetDecimal(0);
            // Consume the complete response so trailing STATISTICS IO messages arrive.
            while (await reader.ReadAsync(ct)) { }
            while (await reader.NextResultAsync(ct)) { while (await reader.ReadAsync(ct)) { } }
            watch.Stop();
            return new(customer, watch.Elapsed.TotalMilliseconds, observed ? reads : null, Convert.ToDecimal(result, CultureInfo.InvariantCulture));
        }
        finally { db.InfoMessage -= handler; }
    }
    private static async Task WaitBoundaryAsync(SqlConnection db, CancellationToken ct)
    {
        using var cmd = new SqlCommand("SELECT SYSUTCDATETIME();", db);
        var now = (DateTime)(await cmd.ExecuteScalarAsync(ct))!;
        var next = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc).AddMinutes(1).AddSeconds(2);
        await Task.Delay(next - now, ct);
    }
    [GeneratedRegex(@"logical reads (\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex ReadsPattern();
}
