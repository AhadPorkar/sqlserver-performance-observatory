/* OPTIONAL: instance-wide diagnostic visibility. DBA approval is required.
   Even though returned rows are filtered, these grants are NOT database isolation.
   Choose only the statement matching your SQL Server version. */
USE [master];
GO
-- SQL Server 2022 and later:
GRANT VIEW SERVER PERFORMANCE STATE TO [ObservatoryReader];
GO
-- SQL Server 2019: uncomment ONLY for that engine.
-- GRANT VIEW SERVER STATE TO [ObservatoryReader];
