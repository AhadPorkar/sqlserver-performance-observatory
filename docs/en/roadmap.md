# Scope and roadmap

[Project](../../README.md) | [Deutsch](../de/roadmap.md)

## Implemented in source
Read-only Query Store/DMV/XEL adapters; weighted comparisons; plan identity comparison; blocked chains including sleeping roots/special owners; normalized deadlock graphs; bounded local persistence and export; shared-key authentication; trilingual static UI; separate guarded SQL laboratory; authored unit/API/browser/SQL acceptance tests and CI.

## Not implemented or not claimed
Raw query/plan viewing, operator-level plan diffs, Query Store wait-stat breakdown, alert delivery, historical browsing UI, automatic remediation, distributed collectors, multi-user RBAC/SSO, Azure-specific connectors, persistent XEL offset cursors, guaranteed low overhead, production acceptance, p95 from aggregates, or AI diagnosis. A plan identity change is not an execution-plan tree diff.

## Suggested milestones
| Milestone | Acceptance, not just a feature list |
|---|---|
| 0.1 validation | Full .NET Windows/Linux build, actual ASP.NET auth/browser tests and SQL Developer lab pass; retain logs. |
| 0.2 operations | Quantify monitoring overhead; restore test; predictable snapshot retention and corruption handling. |
| 0.3 richer evidence | Query Store wait categories and reviewed optional plan retrieval with explicit permission/privacy model. |
| 0.4 scale/access | Measured storage requirements, a proper data store if justified, individual OIDC identities and audited authorization. |
| Later | Bounded cursor recovery, alerts and historical comparison after well-defined false-positive and failure tests. |

No scheduled completion or performance claim is implied. Each milestone should be a small reviewed issue with a reproducible test. Keep diagnosis and production mutation separate.
