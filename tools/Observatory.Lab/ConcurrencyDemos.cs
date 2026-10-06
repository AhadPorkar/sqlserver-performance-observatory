using Microsoft.Data.SqlClient;
using Observatory.Core;
using Observatory.SqlServer;
namespace Observatory.Lab;
public sealed class ConcurrencyDemos(LabEnvironment lab)
{
    private readonly CollectionOptions settings = new() { AllowUntrustedCertificateForLocalLab = true };
    private readonly TargetOptions reader = new() { Id = "lab", DisplayName = "Isolated SQL Server lab", ConnectionEnvironment = "SPO_LAB_READER_CONNECTION", CaptureBlocking = true, CaptureDeadlocks = true };
    public async Task<object> BlockingAsync(CancellationToken ct)
    {
        await using var owner = await lab.OpenAsync(false, ct); await LabEnvironment.CheckMarkerAsync(owner, ct);
        await using var waiter = await lab.OpenAsync(false, ct);
        using var transaction = owner.BeginTransaction();
        using var update = new SqlCommand("UPDATE dbo.DeadlockItems WITH (ROWLOCK) SET Balance=Balance+1 WHERE Id=1;", owner, transaction);
        await update.ExecuteNonQueryAsync(ct);
        using var select = new SqlCommand("SELECT Balance FROM dbo.DeadlockItems WITH (READCOMMITTEDLOCK) WHERE Id=1;", waiter) { CommandTimeout = 30 };
        var waiting = select.ExecuteScalarAsync(ct);
        BoundedResult<BlockingRow>? sample = null;
        try
        {
            var collector = new SqlCollector(settings);
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(300, ct); sample = await collector.ReadBlockingAsync(reader, ct);
                if (sample.Items.Any(r => r.SessionId == waiter.ServerProcessId)) break;
            }
            if (sample is null || !sample.Items.Any(r => r.SessionId == waiter.ServerProcessId))
                throw new InvalidOperationException("Expected blocking not observed; demo did not pass.");
        }
        finally { transaction.Rollback(); await waiting; }
        return new { kind = "measured-local-blocking", timestampUtc = DateTimeOffset.UtcNow,
            rows = sample.Items, chains = BlockingAnalyzer.Build(sample.Items), transactionRolledBack = true };
    }
    public async Task<object> DeadlockAsync(CancellationToken ct)
    {
        await using var check = await lab.OpenAsync(false, ct); await LabEnvironment.CheckMarkerAsync(check, ct);
        var start = DateTimeOffset.UtcNow;
        var first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<int> Actor(int one, int two, TaskCompletionSource<bool> mine, TaskCompletionSource<bool> other)
        {
            await using var db = await lab.OpenAsync(false, ct);
            using var tx = db.BeginTransaction();
            try
            {
                using var cmd = new SqlCommand("UPDATE dbo.DeadlockItems WITH (ROWLOCK) SET Balance=Balance+1 WHERE Id=@Id;", db, tx) { CommandTimeout = 30 };
                var id = cmd.Parameters.Add("@Id", System.Data.SqlDbType.Int); id.Value = one;
                await cmd.ExecuteNonQueryAsync(ct); mine.TrySetResult(true);
                await other.Task.WaitAsync(ct); id.Value = two; await cmd.ExecuteNonQueryAsync(ct);
                tx.Rollback(); return 0;
            }
            catch (Microsoft.Data.SqlClient.SqlException e) when (e.Number == 1205) { return 1205; }
            finally { mine.TrySetResult(true); }
        }
        var outcomes = await Task.WhenAll(Actor(1, 2, first, second), Actor(2, 1, second, first));
        if (outcomes.Count(n => n == 1205) != 1) throw new InvalidOperationException("Expected exactly one deadlock victim.");
        var collector = new SqlCollector(settings);
        var meta = await collector.ReadQueriesAsync(reader, ct);
        DeadlockEvent[] events = [];
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(2000, ct);
            var captured = await collector.ReadDeadlocksAsync(reader, meta.Info.DatabaseId, ct);
            events = captured.Items.Where(e => e.TimestampUtc >= start.AddSeconds(-1)).ToArray();
            if (events.Length > 0) break;
        }
        if (events.Length == 0) throw new InvalidOperationException("Victim observed but no XEL event was read in time. Check XE coverage.");
        return new { kind = "measured-local-deadlock", outcomes, events, note = "Transactions rolled back; no production data was used." };
    }
    public async Task<object> VerifyReaderAsync(CancellationToken ct)
    {
        var collector = new SqlCollector(settings);
        var queries = await collector.ReadQueriesAsync(reader, ct);
        var blocked = await collector.ReadBlockingAsync(reader, ct);
        var deadlocks = await collector.ReadDeadlocksAsync(reader, queries.Info.DatabaseId, ct);
        await using var db = new SqlConnectionFactory(settings).Create(reader); await db.OpenAsync(ct);
        using var role = new SqlCommand("SELECT IS_SRVROLEMEMBER('sysadmin'), IS_MEMBER('db_owner'), HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER');", db);
        await using (var r = await role.ExecuteReaderAsync(ct))
        {
            await r.ReadAsync(ct);
            if (Enumerable.Range(0,3).Any(i => !r.IsDBNull(i) && Convert.ToInt32(r.GetValue(i)) != 0))
                throw new InvalidOperationException("Reader has excessive permissions.");
        }
        foreach (var sql in new[] { "SELECT TOP(1) Total FROM dbo.Orders;", "UPDATE dbo.Orders SET Total=0 WHERE 1=0;" })
        {
            using var cmd = new SqlCommand(sql, db);
            bool denied = false;
            try { await cmd.ExecuteNonQueryAsync(ct); }
            catch (SqlException ex) when (ex.Number == 229) { denied = true; }
            if (!denied) throw new InvalidOperationException("Expected table permission denial.");
        }
        return new { kind = "live-reader-contract", queryRows = queries.Buckets.Length, blockingRows = blocked.Items.Length,
            deadlockEvents = deadlocks.Items.Length, businessTableReadDenied = true, businessTableWriteDenied = true,
            excessiveRolesAbsent = true, databaseId = queries.Info.DatabaseId };
    }
}
