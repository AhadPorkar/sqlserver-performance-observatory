# Security and privacy model

[Project](../../README.md) | [Deutsch](../de/security.md)

## Trust model
This preview is an internal, single-analyst diagnostic service. Anyone with the workspace key can see all configured targets. It has no individual accounts, RBAC, tenant isolation, SSO, MFA or per-user audit trail. Do not expose it as a public multi-tenant service or treat it as a compliance product.

The monitored SQL login, operator configuration, operating system and local state directory are trusted. The browser, login input, database metadata and XEL XML are treated as untrusted inputs. SQL authentication and the filesystem enforce real permissions; UI labels do not.

## Implemented boundaries
- A random 32+ character workspace key is compared using fixed-time hash comparison; it is not sent in URLs or saved in browser storage.
- HttpOnly, SameSite=Strict session cookies, eight-hour non-sliding lifetime and key-version invalidation; HTTPS is required outside loopback. The reverse proxy is explicitly allowlisted.
- Antiforgery validation and exact Origin checks on login/logout; five login requests per IP/minute. API reads require authentication. There is no arbitrary SQL endpoint.
- CSP allows only self-hosted scripts/styles and does not allow unsafe-eval/unsafe-inline. Untrusted data reaches textContent, not HTML injection.
- SQL values are typed parameters; database names/paths are not browser-controlled. Connections encrypt and validate certificates except the explicitly guarded local lab.
- XML DTDs/external resolvers are disabled. XML size, graph size, row counts, request body and stored snapshot size are bounded.

## Data minimization is not anonymization
Query Store text and plan XML are not selected. Query IDs, hashes, plan IDs and aggregate costs are retained. Blocking records omit SQL text, login/host names and object names.

**Raw XEL deadlock XML is read transiently into application memory. It can contain SQL and sensitive values.** Only a whitelisted graph is stored and returned; raw XML, input buffers and names are omitted. A SHA-256 digest supports deduplication, not guaranteed anonymization or legal compliance. Debuggers, process dumps and server-side XEL files remain sensitive. Do not attach real exports to public issues without review.

ASP.NET Data Protection keys are persisted under `state/.keys`. File persistence alone does not encrypt those keys at rest on Linux. Use restrictive ACLs, encrypted disks and an approved key-management integration before a deployment that requires it. Possession of the state directory can compromise sessions. Deleting keys invalidates sessions. Back up the directory securely, never into Git.

## Operational gaps and threat cases
XEL TOP/time predicates limit the result, not necessarily scanned bytes. Treat patterns as administrator input. Certificate validation and database grants are mandatory. A custom sysadmin alias is not prevented merely by rejecting the name `sa`; use the audit script.

The service can reveal cross-database process IDs from a relevant deadlock and instance-level observations through optional diagnostic grants. Shared access keys do not provide individual attribution. File replacement is not a tamper-evident log. No vulnerability scan, penetration test, formal threat-model verification or production load certification has been performed for this source delivery.

Report suspected vulnerabilities privately using GitHub's private vulnerability reporting when enabled. Otherwise request a private contact channel without disclosing the vulnerability or secrets in a public issue.
