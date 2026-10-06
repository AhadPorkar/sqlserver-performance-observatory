# Installation mit minimalen Berechtigungen

[Projekt](../../README.de.md) | [English](../en/permissions.md)

Der DBA erstellt und prüft die Identität. Der Sammler erstellt keine Benutzer, aktiviert Query Store nicht und verändert beim Start keine XE-Sitzungen.

| Funktion | SQL Server 2019 | SQL Server 2022+ |
|---|---|---|
| Verwendete Query-Store-Metadaten | `CONNECT` + `VIEW DATABASE STATE` pro Datenbank. | `CONNECT` + `VIEW DATABASE PERFORMANCE STATE` pro Datenbank. |
| Fremde Sitzungen und serverseitige XEL-Dateien | Optional `VIEW SERVER STATE`. | Optional `VIEW SERVER PERFORMANCE STATE`. |
| Geschäftsdaten lesen/schreiben oder Schema ändern | Nicht erteilen. | Nicht erteilen. |

Serverberechtigungen ermöglichen Einblicke auf Instanzebene. Das nachträgliche Filtern nach Datenbank ersetzt keine Berechtigungsgrenze. Deshalb sind Blocking und Deadlock-Erfassung standardmäßig deaktiviert.

Die Vorlagen für [2022+](../../sql/permissions/query-store-2022-plus.sql), [2019](../../sql/permissions/query-store-2019.sql) und [optionale Serverrechte](../../sql/permissions/optional-server-diagnostics.sql) sind durch den DBA anzupassen. Anschließend [Berechtigungen prüfen](../../sql/permissions/audit-reader.sql).

`sa` wird abgewiesen. Andere Konten oder Gruppen können trotzdem zu weitreichende Rechte besitzen. `ApplicationIntent=ReadOnly` ist lediglich ein Routing-Hinweis, kein Schreibschutz. Keine pauschalen Rollen wie `db_owner`, `db_datareader` oder `sysadmin` vergeben.

SQL-Texte und Plan-XML werden absichtlich nicht gesammelt. Eine [manuelle Query-ID-Abfrage](../../sql/manual/query-id-lookup.sql) kann der DBA mit eigener Berechtigung in SSMS ausführen. Der Labortest `verify-reader` prüft die realen Lesewege und verweigerte Zugriffe auf synthetische Geschäftstabellen; er wurde in dieser Umgebung noch nicht ausgeführt.

Quellen: [Query-Store-Rechte](https://learn.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-query-store-runtime-stats-transact-sql?view=sql-server-ver17), [DMV](https://learn.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-objects/sys-dm-exec-requests-transact-sql?view=sql-server-ver17), [XEL](https://learn.microsoft.com/en-us/sql/relational-databases/system-functions/sys-fn-xe-file-target-read-file-transact-sql?view=sql-server-ver17).
