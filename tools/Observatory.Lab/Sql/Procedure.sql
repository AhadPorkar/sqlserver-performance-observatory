CREATE OR ALTER PROCEDURE dbo.GetCustomerTotal @CustomerId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SUM(Total) AS Total FROM dbo.Orders WHERE CustomerId=@CustomerId OPTION(MAXDOP 1);
END;
