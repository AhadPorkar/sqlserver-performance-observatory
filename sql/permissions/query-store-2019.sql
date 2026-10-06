/* DBA-EXECUTED TEMPLATE: SQL Server 2019 only. Replace identifiers. */
USE [YourApplicationDatabase];
GO
CREATE USER [ObservatoryReader] FOR LOGIN [ObservatoryReader];
GRANT CONNECT TO [ObservatoryReader];
GRANT VIEW DATABASE STATE TO [ObservatoryReader];
GO
