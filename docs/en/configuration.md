# Configuration reference

[Project](../../README.md) | [Deutsch](../de/configuration.md)

Configuration uses standard ASP.NET providers and double-underscore environment keys. Connection values are referenced by environment-variable **name**, never entered through HTTP. Do not commit `.env`, real endpoints/passwords, state files or Data Protection keys.

```powershell
# Example only: use your approved identity and a server certificate trusted by this machine.
$env:SPO_ACCESS_KEY = '<random-32-plus-character-workspace-secret>'
$env:SPO_SQL_PRIMARY = 'Server=sql.internal.example;Database=YourDatabase;Integrated Security=True;Encrypt=True;TrustServerCertificate=False'
$env:Observatory__Mode = 'Live'
$env:Observatory__Targets__0__Id = 'primary'
$env:Observatory__Targets__0__DisplayName = 'Primary database'
$env:Observatory__Targets__0__ConnectionEnvironment = 'SPO_SQL_PRIMARY'
$env:Observatory__Targets__0__CaptureBlocking = 'false'
$env:Observatory__Targets__0__CaptureDeadlocks = 'false'
dotnet run --project src/Observatory.Web --no-launch-profile
```

Windows integrated authentication runs as the service account. Linux integrated authentication requires a separately configured Kerberos environment; the repository does not provision one. SQL authentication is also supported with a dedicated restricted identity.

| Setting below `Observatory` | Default | Meaning |
|---|---|---|
| `Mode` | Replay | Live opens SQL; Replay loads only the labeled fixture. |
| `PublicOrigin` | http://localhost:5080 | Exact browser scheme/host/port; non-loopback must use HTTPS. |
| `StateDirectory` | state | Writable, private, single-process directory. |
| `HistoryLimit` | 48 | Archive files per target; allowed 1..288. |
| `Collection:QueryPeriodSeconds` | 120 | Query Store polling. |
| `Collection:BlockingPeriodSeconds` | 10 | DMV sampling if enabled. |
| `Collection:DeadlockPeriodSeconds` | 120 | XEL scan if enabled. |
| `Collection:LookbackHours` | 24 | Data selection window; not an I/O guarantee. |
| `Collection:CommandTimeoutSeconds` | 5 | Per command/connection setting. |
| `Collection:QueryRowLimit` | 5000 | Query buckets, not distinct queries. |
| `Collection:BlockingRowLimit` | 200 | Request sample bound. |
| `Collection:DeadlockLimit` | 200 | Server-wide event bound before database filtering. |
| `Collection:AllowUntrustedCertificateForLocalLab` | false | Only accepted for allowlisted local host + exact ObservatoryLab database. |
| `Targets:0:XeFilePattern` | system_health*.xel | SQL Server-side path/pattern, not application-server path. |

Configuration ranges are validated in [TargetOptions.cs](../../src/Observatory.SqlServer/TargetOptions.cs). The source reads one extra row to detect truncation. Increasing limits changes resource use; it is not a free fix for incomplete evidence.

For HTTPS behind a private reverse proxy set `PublicOrigin=https://observatory.example.test`, `AllowedHosts=observatory.example.test` and `Observatory__TrustedProxies__0` to the actual trusted proxy IP. Bind Kestrel privately. No wildcard proxy trust. The [Nginx example](../../deploy/nginx.conf.example) is a starting point, not a ready certificate setup.

XEL file patterns are administrator-controlled. Do not accept them from tenants/users or point them at arbitrary shares. Existing `system_health` is read, never created or changed. This preview rescans a bounded time selection instead of maintaining file-offset cursors.
