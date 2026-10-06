-- Closed intervals only. Do not average averages. Each observation is a complete
-- replacement sample: never append these counters to an earlier collection.
SELECT TOP (@Take)
    q.query_id, q.context_settings_id,
    CONVERT(varchar(18), q.query_hash, 1) AS query_hash,
    p.plan_id, CONVERT(varchar(18), p.query_plan_hash, 1) AS plan_hash,
    p.is_forced_plan, i.start_time, i.end_time,
    SUM(rs.count_executions) AS executions,
    SUM(CONVERT(float, rs.count_executions) * rs.avg_duration) / 1000.0 AS total_duration_ms,
    SUM(CONVERT(float, rs.count_executions) * rs.avg_cpu_time) / 1000.0 AS total_cpu_ms,
    SUM(CONVERT(float, rs.count_executions) * rs.avg_logical_io_reads) AS total_logical_reads,
    MAX(rs.max_duration) / 1000.0 AS max_duration_ms
FROM sys.query_store_runtime_stats AS rs
JOIN sys.query_store_runtime_stats_interval AS i ON i.runtime_stats_interval_id = rs.runtime_stats_interval_id
JOIN sys.query_store_plan AS p ON p.plan_id = rs.plan_id
JOIN sys.query_store_query AS q ON q.query_id = p.query_id
WHERE rs.execution_type = 0
  AND rs.count_executions > 0
  AND i.start_time >= @FromUtc
  AND i.end_time <= @ToUtc
  AND q.is_internal_query = 0
GROUP BY q.query_id, q.context_settings_id, q.query_hash, p.plan_id,
         p.query_plan_hash, p.is_forced_plan, i.runtime_stats_interval_id, i.start_time, i.end_time
ORDER BY i.end_time DESC, q.query_id, p.plan_id
OPTION (MAXDOP 1);
