-- Optional DBA-only investigation in SSMS. This may reveal sensitive SQL text.
-- It is NOT an embedded collector query or an HTTP endpoint.
DECLARE @QueryId bigint = 42;
SELECT q.query_id, q.context_settings_id, qt.query_sql_text
FROM sys.query_store_query AS q
JOIN sys.query_store_query_text AS qt ON qt.query_text_id = q.query_text_id
WHERE q.query_id = @QueryId;
