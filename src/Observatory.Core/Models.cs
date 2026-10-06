namespace Observatory.Core;

// Query Store IDs and counts use decimal strings on the HTTP wire; this avoids
// silently rounding SQL bigint values in JavaScript. See JsonDefaults.
public sealed record QueryBucket(long QueryId, long ContextId, string QueryHash,
    long PlanId, string PlanHash, bool IsForcedPlan, DateTimeOffset StartUtc,
    DateTimeOffset EndUtc, long Executions, double TotalDurationMs,
    double TotalCpuMs, double TotalLogicalReads, double MaxDurationMs);

public sealed record QueryStoreInfo(string State, string CaptureMode, int IntervalMinutes,
    long UsedMb, long MaxMb, long ReadOnlyReason, int DatabaseId, int EngineMajor,
    DateTimeOffset ServerUtc, DateTimeOffset? LatestClosedEndUtc);

public sealed record SourceStatus(string State, string Code, DateTimeOffset? LastSuccessUtc,
    DateTimeOffset LastAttemptUtc, double CollectionMs, bool Truncated = false);

public sealed record BlockingRow(int SessionId, int BlockerId, string Status,
    string WaitType, long WaitMs, long ElapsedMs, int OpenTransactions,
    string BlockerStatus, int? BlockerNextId, int? BlockerOpenTransactions);

public sealed record BlockingChain(int SessionId, int[] Path, string Terminal,
    string RootStatus, int? RootOpenTransactions, long WaitMs);

public sealed record DeadlockProcess(string Id, int Spid, int DatabaseId, bool Victim);
public sealed record LockParticipant(string ProcessId, string Mode);
public sealed record DeadlockResource(string Id, string Kind,
    LockParticipant[] Owners, LockParticipant[] Waiters);
public sealed record DeadlockEvent(string Id, DateTimeOffset TimestampUtc,
    DeadlockProcess[] Processes, DeadlockResource[] Resources);

public sealed record Snapshot(int SchemaVersion, string Id, string TargetId, string DisplayName,
    string Mode, DateTimeOffset CapturedUtc, QueryStoreInfo? QueryStore,
    SourceStatus QueriesStatus, SourceStatus BlockingStatus, SourceStatus DeadlocksStatus,
    QueryBucket[] Buckets, BlockingRow[] Blocking, DeadlockEvent[] Deadlocks)
{
    public string? TargetFingerprint { get; init; }
}

public sealed record WindowStats(long Executions, double? MeanMs, double TotalMs,
    double? MeanCpuMs, double? MeanReads, double MaxMs, long[] Plans);
public sealed record QueryComparison(long QueryId, string QueryHash, long ContextId,
    WindowStats Baseline, WindowStats Recent, double? Ratio, double? DeltaMs,
    double? EstimatedExtraMs, bool PlanSetChanged, string Classification,
    string[] EvidenceCodes);
public sealed record AnalysisReport(DateTimeOffset? BaselineStartUtc,
    DateTimeOffset? RecentStartUtc, DateTimeOffset? EndUtc,
    int BaselineMinutes, int RecentMinutes, QueryComparison[] Queries,
    string[] Warnings, bool ComparisonsEnabled);

public sealed record AnalysisSettings(int BaselineMinutes = 60, int RecentMinutes = 60,
    int MinimumExecutions = 10, double MinimumRatio = 2,
    double MinimumDeltaMs = 25, double MinimumBaselineMs = 1);
