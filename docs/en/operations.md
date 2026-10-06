# Operations and recovery

[Project](../../README.md) | [Deutsch](../de/operations.md)

## Runbook
Run one process under a dedicated OS identity. Keep configuration and secrets outside the repository. Grant write access only to its state directory. Bind the application privately behind HTTPS and explicitly trust only the proxy that terminates TLS. Maintain system clock synchronization; source Query Store/XEL windows use server timestamps, while collection timestamps and retention use application time.

`/health/live` only proves the HTTP process is alive. It does not certify SQL connectivity, freshness or complete coverage. Inspect source `state`, `code`, `lastSuccessUtc`, `lastAttemptUtc`, duration and `truncated` independently. A target may have current blocking observations but old Query Store data. Do not use the snapshot-wide timestamp as a replacement for each source's timestamp.

| State | Operator response |
|---|---|
| `disabled` | Confirm intentional opt-out or Query Store OFF/ERROR. No zero-health inference. |
| `pending` / `unavailable` | Wait for collection or resolve target identity/configuration. |
| `error` / `PERMISSION_DENIED` | Audit version-specific grants; never solve by making the account sysadmin. |
| `error` / `TIMEOUT` | Inspect source load/permissions/storage. Increase polling interval before raising limits. |
| `truncated` | Comparison is incomplete. Review window/limits and measure added overhead. |
| Host stops after storage error | Correct disk permissions/space; preserve evidence of the fault. |

## Resource budget
Defaults: one serial collection per target, 1..8 targets, SQL pool limit four per target connection string, five-second command timeout, low deadlock priority and a one-second lock timeout on the collector session. These session settings are not changes to global server configuration. A 24-hour lookback with 5,000 bucket limit can truncate a busy workload. Collection duration is shown, but it is not a server CPU/IO cost measurement. Benchmark monitoring overhead in your own environment.

The XEL implementation rescans existing files for a time range; it has no persistent cursor. File rotation, poll outages, oversized records, permissions and caps can lose evidence. Never alter the built-in system_health session through this application. [Microsoft guidance](https://learn.microsoft.com/en-us/sql/relational-databases/extended-events/use-the-system-health-session?view=sql-server-ver17).

## Backup and restore
Stop the service, copy the entire state directory to encrypted private storage, then restart. Test restoration into a separate stopped installation with the same schema version and correct permissions. Do not run two instances against one directory. Protect `.keys`; lost or compromised keys require session invalidation and incident handling. State is diagnostic history, not a backup of monitored databases.

Default archives: 48 snapshots per target, approximately 12 hours at 15-minute cadence. Each file is bounded to 32 MiB; storage usage scales with row limits and targets. Latest records retain up to 500 sanitized deadlocks from seven days; XEL selection initially looks back 24 hours. There is no guarantee of complete retention. Orphaned temporary files after a hard crash can be removed only while stopped and after identifying them. Power-loss durability and network filesystem semantics require separate acceptance.

Upgrade only after backing up, passing CI and reading release notes. Schema version 1 is checked; there is no automatic migration between unknown future schemas. A database restored/recreated at the same configured endpoint requires explicit state review.
