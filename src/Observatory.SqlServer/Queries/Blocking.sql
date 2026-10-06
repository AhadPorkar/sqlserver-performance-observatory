-- Only waiting requests in this database; direct blocker metadata may refer to
-- another database. Sleeping blockers are visible through dm_exec_sessions.
SELECT TOP (@Take)
    r.session_id, r.blocking_session_id, r.status, COALESCE(r.wait_type, '') AS wait_type,
    CONVERT(bigint, r.wait_time) AS wait_ms, CONVERT(bigint, r.total_elapsed_time) AS elapsed_ms,
    s.open_transaction_count,
    COALESCE(bs.status, '') AS blocker_status,
    br.blocking_session_id AS blocker_next_id,
    bs.open_transaction_count AS blocker_open_transactions
FROM sys.dm_exec_requests AS r
JOIN sys.dm_exec_sessions AS s ON s.session_id = r.session_id
LEFT JOIN sys.dm_exec_sessions AS bs ON bs.session_id = r.blocking_session_id
OUTER APPLY (
    SELECT TOP (1) b.blocking_session_id FROM sys.dm_exec_requests AS b
    WHERE b.session_id = r.blocking_session_id ORDER BY b.request_id
) AS br
WHERE r.database_id = DB_ID() AND r.blocking_session_id <> 0
  AND r.session_id <> @@SPID AND s.is_user_process = 1
ORDER BY r.wait_time DESC, r.session_id
OPTION (MAXDOP 1);
