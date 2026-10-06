-- LAB ONLY. Program.cs checks local host, Developer edition, fixed database name
-- and the project marker before executing this resource.
IF OBJECT_ID(N'dbo.ObservatoryLabMarker', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ObservatoryLabMarker(Id int NOT NULL PRIMARY KEY, Project nvarchar(100) NOT NULL);
    INSERT dbo.ObservatoryLabMarker VALUES(1, N'sqlserver-performance-observatory');
END;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders(
        OrderId int NOT NULL CONSTRAINT PK_ObservatoryOrders PRIMARY KEY CLUSTERED,
        CustomerId int NOT NULL, Total decimal(12,2) NOT NULL,
        CreatedAt datetime2(0) NOT NULL, Payload char(160) NOT NULL);
    ;WITH D AS (SELECT v FROM (VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) x(v)),
    N AS (SELECT 1+a.v+10*b.v+100*c.v+1000*d.v+10000*e.v AS n
          FROM D a CROSS JOIN D b CROSS JOIN D c CROSS JOIN D d CROSS JOIN D e)
    INSERT dbo.Orders(OrderId, CustomerId, Total, CreatedAt, Payload)
    SELECT n, 2+(n%1999), CAST((n*17)%10000 AS decimal(12,2))/100,
           DATEADD(MINUTE,n,CAST('2025-01-01' AS datetime2)), REPLICATE('x',160) FROM N;
END;
IF OBJECT_ID(N'dbo.DeadlockItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeadlockItems(Id int NOT NULL PRIMARY KEY, Balance int NOT NULL);
    INSERT dbo.DeadlockItems VALUES(1,100),(2,100);
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Orders') AND name=N'IX_Orders_CustomerId')
    CREATE INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId) INCLUDE(Total);
