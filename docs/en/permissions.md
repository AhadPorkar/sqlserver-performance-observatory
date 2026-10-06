# Least-privilege installation

[Project](../../README.md) | [Deutsch](../de/permissions.md)

## Two intentionally separate permission tiers
The DBA creates the login/contained identity and reviews grants. The collector does not create users, enable Query Store or alter Extended Events on startup.

| Capability | SQL Server 2019 | SQL Server 2022+ |
|---|---|---|
| Query Store metadata used by this project | `CONNECT` and `VIEW DATABASE STATE` in each target. | `CONNECT` and `VIEW DATABASE PERFORMANCE STATE` in each target. |
| Other sessions / existing server XEL reads | Optional `VIEW SERVER STATE`. | Optional `VIEW SERVER PERFORMANCE STATE`. |
| Application-table read/write or schema change | Not granted. | Not granted. |

The database grants are documented for the [runtime view](https://learn.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-query-store-runtime-stats-transact-sql?view=sql-server-ver17). Server requirements are documented for [requests](https://learn.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-objects/sys-dm-exec-requests-transact-sql?view=sql-server-ver17) and [XEL file reads](https://learn.microsoft.com/en-us/sql/relational-databases/system-functions/sys-fn-xe-file-target-read-file-transact-sql?view=sql-server-ver17).

**Server-level diagnostic grants expose instance-level information.** Filtering returned records by database is not a SQL Server authorization boundary. Blocking and deadlock capture are opt-in and off by default. A server permission check prevents a denied DMV from being mistaken for an empty healthy sample. The web installation itself has one trusted analyst identity, not per-database role separation.

## DBA procedure
Review the version-specific [2022+ template](../../sql/permissions/query-store-2022-plus.sql) or [2019 template](../../sql/permissions/query-store-2019.sql). Replace the explicit database/login placeholders. Review the [optional server grant](../../sql/permissions/optional-server-diagnostics.sql) separately. Verify with [audit-reader.sql](../../sql/permissions/audit-reader.sql).

Do not grant `db_owner`, `db_datareader`, `sysadmin`, `CONTROL SERVER`, `ALTER`, plan-forcing permissions or business-table rights merely to make errors disappear. The application rejects the literal `sa` username, but an alias or group can still possess excessive rights: the audit remains mandatory. `ApplicationIntent=ReadOnly` is a routing hint, **not** a permission restriction.

Text retrieval from `sys.query_store_query_text` has separate server permissions on the documented versions. This collector deliberately omits query text and plan XML. A DBA can use the [manual query-ID lookup](../../sql/manual/query-id-lookup.sql) in SSMS under their own approved access, keeping sensitive text out of the browser.

## Acceptance contract
The isolated lab's `verify-reader` command actually tries the configured collector reads, checks excessive roles, and verifies denial of SELECT/UPDATE on its synthetic business table. It must run successfully before advertising that the grant profile was tested. It is not a formal proof against all inherited/custom grants.
