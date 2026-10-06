-- Read-only metadata. No database name is interpolated into SQL.
SELECT CAST(SERVERPROPERTY('ProductMajorVersion') AS int) AS engine_major,
       DB_ID() AS database_id,
       CAST(SYSUTCDATETIME() AS datetimeoffset) AS server_utc,
       CAST(HAS_PERMS_BY_NAME(NULL, 'SERVER', 'VIEW SERVER STATE') AS int) AS legacy_server_state;
SELECT actual_state_desc, query_capture_mode_desc, interval_length_minutes,
       current_storage_size_mb, max_storage_size_mb, readonly_reason,
       (SELECT MAX(end_time) FROM sys.query_store_runtime_stats_interval
        WHERE end_time <= CAST(SYSUTCDATETIME() AS datetimeoffset)) AS latest_closed_end
FROM sys.database_query_store_options;
