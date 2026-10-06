/* DBA-EXECUTED TEMPLATE. Edit identifiers in brackets, then review.
   Never execute this using the monitoring application's connection.
   Create an appropriate LOGIN (Windows/SQL identity) outside this file.
   No SELECT on application tables, db_owner, sysadmin or ALTER is required. */
USE [YourApplicationDatabase];
GO
CREATE USER [ObservatoryReader] FOR LOGIN [ObservatoryReader];
GO
GRANT CONNECT TO [ObservatoryReader];
GRANT VIEW DATABASE PERFORMANCE STATE TO [ObservatoryReader];
GO
-- Query SQL text is deliberately not joined or returned by this application.
-- Server-scoped blocking/XE permissions are OPTIONAL and separate.
