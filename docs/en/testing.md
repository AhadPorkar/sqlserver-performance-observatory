# Verification status and acceptance

[Project](../../README.md) | [Deutsch](../de/testing.md)

## CI follow-up: 6 October 2026
User-supplied GitHub logs now confirm successful .NET build/publish and 51 passing
xUnit tests on **each** of Windows/Linux, plus 20 real ASP.NET API checks. The
native browser suite failed in an incorrectly scoped CSP assertion. This update
corrects the test; it does not disable CSP. [Diagnosis, regression controls and
remaining acceptance scope](ci-csp-fix.md). Original preparation results below
remain a historical record, not a statement that the supplied CI never ran.

## Delivery status: 5 October 2026
This is source release `0.1.0-preview.1`. No .NET SDK, Docker or SQL Server was available in the source-delivery environment. Network/DNS prevented SDK acquisition. Therefore **no C# compilation, xUnit execution, ASP.NET runtime, real SQL collection, lab benchmark, Windows execution or container launch is claimed for this delivery**. Authored tests are not passed tests.

Locally exercised: Node built-in frontend tests; Python fixture/support tests; source/package checks; Chromium UI interactions through an explicitly isolated DOM/HTTP fixture bridge. The bridge runs the actual HTML/CSS/JS against synthetic responses. Native navigation to localhost was blocked by the environment (`ERR_BLOCKED_BY_ADMINISTRATOR`). The bridge does not qualify browser cookies, Origin enforcement, antiforgery, CSP, downloads or real reload behavior. Its synthetic login is not ASP.NET authentication.

The machine-readable [delivery validation](../qa/delivery-validation.json) lists actual counts and limitations. [UI report](../qa/ui-fixture-report.json) identifies the transport. No measured SQL speedup is included.

## Commands
```bash
python scripts/check_repository.py
npm run check
npm test
python tests/python_test.py
dotnet test tests/Observatory.Tests/Observatory.Tests.csproj -c Release
```

Run API/browser tests only after starting the **real** ASP.NET application in Replay mode with `SPO_ACCESS_KEY` set:
```bash
python tests/api_smoke.py
pip install -r tests/requirements.txt
python -m playwright install chromium
python tests/ui_test.py --base-url http://localhost:5080
```

Negative API login tests consume the login rate limit; wait for the next one-minute window before browser login tests. `--fixture --dom-bridge` is explicitly a local presentation harness, never the CI default. Do not add `bypass_csp`, unsafe-eval, skipped assertions or automatic fixture fallback to make a failing native run green.

## Authored .NET cases
The xUnit source covers weighted means, sample thresholds, closed/crossing intervals, repeated polling, plan correlation, unknown data, exact bigint serialization, sleeping blockers, special owner IDs, cycles, XML privacy/DTD/size, file lease/retention, embedded SQL restrictions and connection certificate/sa safeguards. These must execute on the real .NET runtime.

## GitHub Actions
`Verify` checks source and UI units, builds/tests on Windows and Linux, then separately exercises authentication and native Chromium against a real ASP.NET process. Trace, screenshot and API logs are uploaded on failure. The manual SQL Server acceptance workflow starts a dedicated Developer container, runs setup, aligned benchmark, blocking, deadlock and reader-contract checks. It requires explicit license acceptance and preserves actual evidence/image identity.

## Acceptance before pilot use
A successful full build; native login/CSRF/origin/CSP/logout; minimal reader grants on intended SQL version; source outage and reconnect; Query Store OFF/READ_ONLY/truncation; repeated polling without inflated counts; known sleeping-blocker and deadlock events; workload result equivalence; parsed logical reads; storage-full/corrupt-state behavior; secure backup/restore; certificate rejection; representative monitoring-overhead measurement. Record exact versions and commit. SQL Server 2019/2022 compatibility remains unverified until those runs pass. A green Replay workflow alone is not a SQL Server acceptance result.
