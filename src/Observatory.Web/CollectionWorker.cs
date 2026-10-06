using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Observatory.Core;
using Observatory.SqlServer;
namespace Observatory.Web;

public sealed class CollectionWorker(ObservatoryOptions options, SnapshotCatalog catalog,
    SnapshotStore store, SqlCollector collector, ILogger<CollectionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Mode == "Replay")
        {
            var file = Path.Combine(AppContext.BaseDirectory, "fixtures", "replay.json");
            await using var input = File.OpenRead(file);
            var snapshot = await JsonSerializer.DeserializeAsync<Snapshot>(input, JsonDefaults.Create(), stoppingToken)
                ?? throw new InvalidDataException("Invalid replay fixture.");
            if (snapshot.Mode != "Replay" || snapshot.SchemaVersion != 1) throw new InvalidDataException("Not a synthetic replay fixture.");
            catalog.Put(snapshot); // Never rewrite timestamps to make the fixture appear live.
            return;
        }
        await Task.WhenAll(options.Targets.Select(t => RunTargetAsync(t, stoppingToken)));
    }
    private async Task RunTargetAsync(TargetOptions target, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        SourceStatus pending = new("pending", "NOT_COLLECTED", null, now, 0);
        using var identityConnection = new SqlConnectionFactory(options.Collection).Create(target);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identityConnection.DataSource + "|" + identityConnection.Database)));
        var persisted = await store.LatestAsync(target.Id, ct);
        if (persisted?.TargetFingerprint != fingerprint) persisted = null;
        var snapshot = persisted ?? new Snapshot(1, Guid.NewGuid().ToString("N"), target.Id,
            target.DisplayName, "Live", now, null, pending, pending, pending, [], [], []);
        // A restarted collector must not report old, persisted data as freshly healthy.
        snapshot = snapshot with
        {
            DisplayName = target.DisplayName,
            TargetFingerprint = fingerprint,
            QueriesStatus = snapshot.QueriesStatus with { State = "pending", Code = "AWAITING_FRESH_SAMPLE" },
            BlockingStatus = snapshot.BlockingStatus with { State = "pending", Code = "AWAITING_FRESH_SAMPLE" },
            DeadlocksStatus = snapshot.DeadlocksStatus with { State = "pending", Code = "AWAITING_FRESH_SAMPLE" }
        };
        catalog.Put(snapshot);
        var queryDue = DateTimeOffset.MinValue; var blockingDue = queryDue; var xeDue = queryDue;
        var archiveDue = queryDue; int queryErrors = 0, blockErrors = 0, xeErrors = 0;
        while (!ct.IsCancellationRequested)
        {
            bool changed = false; now = DateTimeOffset.UtcNow;
            if (now >= queryDue)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var result = await collector.ReadQueriesAsync(target, ct);
                    var code = result.Info.State is "OFF" or "ERROR" ? "QUERY_STORE_NOT_CAPTURING" : result.Truncated ? "ROW_LIMIT_REACHED" : "OK";
                    snapshot = snapshot with { QueryStore = result.Info, Buckets = result.Buckets,
                        QueriesStatus = new(code == "OK" ? "ok" : code == "ROW_LIMIT_REACHED" ? "truncated" : "disabled", code,
                            DateTimeOffset.UtcNow, now, sw.Elapsed.TotalMilliseconds, result.Truncated) };
                    queryErrors = 0;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex) { snapshot = snapshot with { QueriesStatus = Failure(snapshot.QueriesStatus, ex, now, sw) }; queryErrors++; }
                queryDue = DateTimeOffset.UtcNow.AddSeconds(Delay(options.Collection.QueryPeriodSeconds, queryErrors)); changed = true;
            }
            if (now >= blockingDue)
            {
                var sw = Stopwatch.StartNew();
                if (!target.CaptureBlocking) snapshot = snapshot with { Blocking = [], BlockingStatus = new("disabled", "SERVER_DIAGNOSTICS_OPT_IN", null, now, 0) };
                else
                {
                    try
                    {
                        var result = await collector.ReadBlockingAsync(target, ct);
                        snapshot = snapshot with { Blocking = result.Items, BlockingStatus = Success(now, sw, result.Truncated) }; blockErrors = 0;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch (Exception ex) { snapshot = snapshot with { BlockingStatus = Failure(snapshot.BlockingStatus, ex, now, sw) }; blockErrors++; }
                }
                blockingDue = DateTimeOffset.UtcNow.AddSeconds(Delay(options.Collection.BlockingPeriodSeconds, blockErrors)); changed = true;
            }
            if (now >= xeDue)
            {
                var sw = Stopwatch.StartNew();
                if (!target.CaptureDeadlocks) snapshot = snapshot with { Deadlocks = [], DeadlocksStatus = new("disabled", "SERVER_DIAGNOSTICS_OPT_IN", null, now, 0) };
                else if (snapshot.QueryStore is null || snapshot.QueriesStatus.State == "error")
                    snapshot = snapshot with { DeadlocksStatus = new("unavailable", "DATABASE_IDENTITY_UNAVAILABLE", snapshot.DeadlocksStatus.LastSuccessUtc, now, 0) };
                else
                {
                    try
                    {
                        var result = await collector.ReadDeadlocksAsync(target, snapshot.QueryStore.DatabaseId, ct);
                        var events = result.Items.Concat(snapshot.Deadlocks).Where(x => x.TimestampUtc >= DateTimeOffset.UtcNow.AddDays(-7))
                            .DistinctBy(x => x.Id).OrderByDescending(x => x.TimestampUtc).Take(500).ToArray();
                        snapshot = snapshot with { Deadlocks = events, DeadlocksStatus = Success(now, sw, result.Truncated) }; xeErrors = 0;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch (Exception ex) { snapshot = snapshot with { DeadlocksStatus = Failure(snapshot.DeadlocksStatus, ex, now, sw) }; xeErrors++; }
                }
                xeDue = DateTimeOffset.UtcNow.AddSeconds(Delay(options.Collection.DeadlockPeriodSeconds, xeErrors)); changed = true;
            }
            if (changed)
            {
                snapshot = snapshot with { CapturedUtc = DateTimeOffset.UtcNow, Id = Guid.NewGuid().ToString("N") };
                // Persist before publishing; storage failures stop this hosted service.
                // ASP.NET Core's default background exception policy stops the host.
                bool archive = now >= archiveDue;
                await store.SaveAsync(snapshot, archive, ct); catalog.Put(snapshot);
                if (archive) archiveDue = DateTimeOffset.UtcNow.AddMinutes(15);
            }
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }
    private SourceStatus Failure(SourceStatus previous, Exception ex, DateTimeOffset now, Stopwatch sw)
    {
        string code = SqlCollector.ErrorCode(ex);
        logger.LogWarning("Collection failed: {Code}; exception type {Type}. No source message is logged.", code, ex.GetType().Name);
        return new("error", code, previous.LastSuccessUtc, now, sw.Elapsed.TotalMilliseconds);
    }
    private static SourceStatus Success(DateTimeOffset attempt, Stopwatch sw, bool truncated)
        => new(truncated ? "truncated" : "ok", truncated ? "ROW_LIMIT_REACHED" : "OK", DateTimeOffset.UtcNow, attempt, sw.Elapsed.TotalMilliseconds, truncated);
    private static double Delay(int period, int failures) => failures == 0 ? period
        : Math.Min(900, period * Math.Pow(2, Math.Min(failures, 5))) + Random.Shared.NextDouble() * 3;
}
