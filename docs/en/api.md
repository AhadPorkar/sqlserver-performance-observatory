# HTTP API contract

[Project](../../README.md) | [Deutsch](../de/api.md)

All paths are same-origin. Except static UI, `/auth/csrf` and liveness, API access requires the session cookie. There is no public read endpoint or arbitrary SQL execution endpoint.

| Method/path | Contract |
|---|---|
| GET `/health/live` | HTTP process liveness only. |
| GET `/auth/csrf` | Antiforgery cookie and JSON `token`. |
| POST `/auth/login` | JSON `{"accessKey":"..."}`, exact Origin and X-CSRF-TOKEN; sets session cookie. |
| POST `/auth/logout` | Fresh antiforgery token + session; clears cookie. |
| GET `/api/targets` | Mode and ready target descriptors. |
| GET `/api/targets/{id}/snapshot` | Schema 1 snapshot with independent source statuses. |
| GET `/api/targets/{id}/analysis` | Weighted baseline/recent comparisons. Optional `baselineMinutes` and `recentMinutes`, each 1..1440, combined within configured lookback. |
| GET `/api/targets/{id}/blocking` | Source status and derived chains. |
| GET `/api/targets/{id}/evidence` | Downloadable **unsigned** diagnostic JSON including snapshot, analysis and chains. |

SQL bigint IDs, counters and selected sizes are decimal **strings** in JSON. Preserve them as strings or BigInt in clients. A number beyond JavaScript's safe integer range must not be rounded. Timestamps are UTC/offset ISO 8601. Missing means/ratios are null, not zero.

`classification` is `candidate`, `stable`, `insufficient` or `incomplete`; stable only means the heuristic thresholds were not exceeded, not that the query is fast or healthy. A plan-set flag is not a causal diagnosis. Source status may be `ok`, `disabled`, `pending`, `error`, `unavailable` or `truncated`.

The UI uses fixed 60+60-minute defaults. Alternate API windows do not change source polling or Query Store configuration. No API exists for login provisioning, configuration writes, raw SQL/plan retrieval, XEL upload, KILL, index changes, plan forcing, historical archive browsing, alert subscriptions or tuning.

The export is not signed, tamper-evident or an attestation. It contains diagnostic metadata and may be sensitive despite omission of query text. Review it before sharing.
