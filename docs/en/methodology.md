# Measurement methodology

[Project](../../README.md) | [Deutsch](../de/methodology.md)

## What a duration actually means
Query Store exposes aggregated runtime statistics, not every individual execution. Runtime duration and CPU values are expressed in microseconds by SQL Server; this collector converts them to milliseconds once. Logical reads are counts of 8-KB pages, not bytes or physical disk I/O. The catalog may contain multiple runtime rows for the same plan/interval/execution type. The SQL groups those rows before analysis. See the [official runtime-statistics reference](https://learn.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-query-store-runtime-stats-transact-sql?view=sql-server-ver17).

For bucket `i`, with execution count `n_i` and average duration `d_i`:

```text
TotalDurationMs = SUM(n_i * d_i) / 1000
WeightedMeanMs = TotalDurationMs / SUM(n_i)
Ratio = RecentWeightedMean / BaselineWeightedMean
EstimatedExtraElapsedMs = MAX(0, RecentMean - BaselineMean) * RecentCount
```

The last value is an explanatory estimate, not CPU consumption, money saved or proven causality. Example: 100 executions at 10 ms and one at 1,000 ms average **19.802 ms**, not 505 ms.

## Window definition
The analysis ends at the latest closed Query Store interval. By default, the previous 60 minutes are the recent window and the preceding 60 minutes the baseline. Buckets are indivisible: a bucket crossing a boundary is excluded, not prorated. A configured capture interval that does not divide the requested window creates a warning. An open interval is excluded even if it already contains data.

Polling repeats a replacement snapshot, never an incremental addition of the same counters. Query identity includes query ID, context settings ID and hash. Plans are compared as sorted ID sets. A changed plan set and a slower mean are **correlated observations**, not proof that a plan caused the slowdown. Parameters, load, cache state, waits, memory grants and capture policies can differ. Parameter-sensitive/multiple-plan behavior is not automatically an error.

## Candidate rule
Both windows need at least 10 successful executions. The baseline mean must be at least 1 ms; the recent/baseline ratio at least 2; the difference at least 25 ms. A source error or row truncation suppresses candidate classification. Low samples are `insufficient`, not healthy. These defaults are heuristics, not a hypothesis test or confidence interval.

Only `execution_type=0` successful, captured, noninternal Query Store executions enter the metrics. Aborted and failed execution counts are not included. AUTO/CUSTOM capture can omit queries. An empty result does not prove an idle or healthy database. OFF/ERROR states, READ_ONLY capture limitations, source times and truncation must be inspected. See [Query Store management](https://learn.microsoft.com/en-us/sql/relational-databases/performance/manage-the-query-store?view=sql-server-ver17).

## Why no Query Store p95
A weighted mean and maximum do not define the distribution of execution latency. This application deliberately does not derive p95/p99 from those aggregates. The separate lab can report median/p95 because it records individual client stopwatch samples; those samples include driver/network cost and are not interchangeable with server duration.

## Blocking and deadlocks
Blocking is a point-in-time DMV sample, not a count of every incident between polls. Sleeping sessions may be roots. Negative blocker IDs are special SQL Server owners, not sessions; unknown/out-of-database chains are marked incomplete. Deadlocks are parsed from bounded existing XEL data and deduplicated; retention and polling gaps mean the count is not a complete audit. Query regressions are not automatically joined causally to a concurrent blocking sample.

See [official request DMV semantics](https://learn.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-objects/sys-dm-exec-requests-transact-sql?view=sql-server-ver17).
