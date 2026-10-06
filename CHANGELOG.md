# Changelog

[Deutsch](CHANGELOG.de.md)

## Unreleased - CI correction, 2026-10-06
- Replace inspector-exempt eval assertion with an explicitly CSP-respecting CDP probe.
- Add 12 positive/negative controls, including an independent native-click check.
- Verify the actual document header; retain partial UI/CSP evidence on failure.
- Record the supplied successful .NET/API CI results with their original scope.
- Keep application code, SQL, dependencies and production security policy unchanged.

## 0.1.0-preview.1 - 2026-10-05
Initial source delivery: read-only Query Store/DMV/XEL adapters, weighted comparisons, source coverage, blocking and normalized deadlocks, shared-key session API, English/German/Persian UI, bounded local snapshots, guarded Developer lab, bilingual documentation and authored CI/tests.

Actual preparation checks and unexecuted runtime scope are recorded in docs/en/testing.md. No production deployment or measured SQL speedup is included.
