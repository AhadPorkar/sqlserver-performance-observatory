# Reproducible local SQL Server laboratory

[Project](../../README.md) | [Deutsch](../de/lab-guide.md)

## Safety boundary: this tool deliberately writes
The web application never calls the lab. The lab executable requires `--accept-lab-only`, an allowlisted local hostname, exact database name `ObservatoryLab`, SQL Server **Developer** edition, and a project marker before changing an existing database. A local port forwarded to another server is still your responsibility; the guard is not proof of network topology.

Use a dedicated disposable instance, never a valuable local database or production. Setup creates its own login, tables and Query Store configuration. Do not copy the lab account's privileged connection into the web collector. The scripts refuse to adopt an existing unmarked database.

## Start a disposable instance
Docker/Compose and sufficient SQL Server resources are required. Read the SQL Server license; Developer edition is for development/test, not production. [Official container guide](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver17&tabs=cli).

```powershell
Set-Location 'D:\GITHUB\sqlserver-performance-observatory'
# Run only after accepting the Developer license for a non-production lab.
$env:SPO_ACCEPT_SQL_EULA = 'Y'
function New-LabSecret {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return 'Lab9!' + [Convert]::ToBase64String($bytes)
}
$env:SPO_LAB_SA_PASSWORD = New-LabSecret
$env:SPO_LAB_READER_PASSWORD = New-LabSecret
docker compose -f compose.lab.yml up -d --wait --wait-timeout 300
if ($LASTEXITCODE -ne 0) { throw 'SQL Server did not become healthy.' }
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-lab.ps1 -Command setup
if ($LASTEXITCODE -ne 0) { throw 'Lab setup failed.' }
```

Keep these environment secrets for the lifetime of that volume. An existing login password is not silently rotated on repeated setup. Losing the temporary password requires deliberate recreation of the disposable volume; do not keep retrying with newly generated passwords against the old instance.

## The three-phase experiment
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-lab.ps1 -Command benchmark -AlignIntervals
if ($LASTEXITCODE -ne 0) { throw 'Benchmark did not pass.' }
```

The controlled dataset has 100,000 orders and a single stored procedure. Each phase uses the identical five-parameter sequence, five warmups and thirty measured executions:

| Phase | Controlled change | Evidence |
|---|---|---|
| Baseline | Covering customer index present. | Individual elapsed samples and logical reads. |
| Induced regression | That lab index is removed. | Same query and parameter sequence. |
| Repair | The same index is restored. | Same samples; result sequence must match. |

The index is restored in `finally` on ordinary execution failure. A killed process or power failure can interrupt cleanup: rerun setup on the dedicated lab before reuse. No server-wide cache flush is used. Removing/recreating the index changes compilation and plan behavior deliberately; the warmups run in every phase.

`-AlignIntervals` waits at one-minute Query Store boundaries so the measured phases do not overlap the same closed interval. Allow several minutes. Without it, client measurements still run, but Query Store intervals can overlap. The report explicitly identifies that mode.

Individual measurements record client elapsed milliseconds (including driver/network cost), parsed `STATISTICS IO` logical reads for Orders and scalar results. Median/p95 use nearest rank over thirty samples; they are small-sample local measurements. The tool also exports collected Query Store buckets and SQL Server version. Buckets can include warmups and other lab commands; they are **not** automatically the same thirty measured samples.

No real speedup is prefilled. A fast machine may not cross the dashboard's 25-ms default threshold even when read amplification is clear. That is not a reason to fabricate latency or force a candidate label. Preserve the raw evidence and state the environment.

## Concurrency and permission acceptance
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-lab.ps1 -Command blocking
if ($LASTEXITCODE -ne 0) { throw 'Blocking not observed.' }
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-lab.ps1 -Command deadlock
if ($LASTEXITCODE -ne 0) { throw 'Deadlock/XEL acceptance failed.' }
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-lab.ps1 -Command verify-reader
if ($LASTEXITCODE -ne 0) { throw 'Reader acceptance failed.' }
```

Blocking holds one synthetic row in an open transaction, observes the waiting session through the restricted reader, then rolls back. Deadlock actors lock opposite rows, synchronize, and compete for each other's row. Acceptance requires exactly one 1205 victim **and** an XEL record within the polling deadline. Missing XEL evidence is a failure, not a skip. Transactions roll back.

`verify-reader` executes all collector sources using the restricted account, rejects excessive roles and expects business-table SELECT and UPDATE to be denied. JSON reports are written under ignored `artifacts/`. Capture CPU/RAM, SQL version, container image digest and workload settings with any published results.

## Inspect the local lab in the dashboard
In a separate shell, set a random workspace key, `Observatory__Mode=Live`, `SPO_SQL_PRIMARY` to the **reader** connection, and `Observatory__Collection__AllowUntrustedCertificateForLocalLab=true`. Enable `CaptureBlocking`/`CaptureDeadlocks` only for this lab. `Database=ObservatoryLab` and local hostname are mandatory for the certificate exception. Default UI windows are 60+60 minutes; the API permits explicit smaller windows, but the three phases are not a continuously running production baseline.

## Clean up
```powershell
docker compose -f compose.lab.yml down -v
```

This removes the dedicated Compose lab volume, including the synthetic data. Do not substitute another Compose file. The reference tag `2022-latest` is mutable; the acceptance workflow records the actual pulled image digest. The source-delivery environment did not run Docker or SQL Server; do not cite the illustrative UI figures as laboratory results.
