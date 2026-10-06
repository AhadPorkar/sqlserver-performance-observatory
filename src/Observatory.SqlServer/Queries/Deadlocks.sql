-- Filename is an administrator-set SQL SERVER path, never a web parameter.
-- WHERE/TOP bound returned data, NOT all I/O performed by the XEL reader.
-- Re-read a bounded lookback and deduplicate sanitized events; rollover gaps
-- cannot be recovered. There is deliberately no fragile file-offset cursor.
SELECT TOP (@Take) timestamp_utc, event_data
FROM sys.fn_xe_file_target_read_file(@Pattern, NULL, NULL, NULL)
WHERE object_name = N'xml_deadlock_report'
  AND timestamp_utc >= DATEADD(HOUR, -@LookbackHours, SYSUTCDATETIME())
ORDER BY timestamp_utc DESC, file_name DESC, file_offset DESC;
