namespace Observatory.SqlServer;
public sealed class TargetOptions
{
    public string Id { get; set; } = "primary";
    public string DisplayName { get; set; } = "Primary database";
    public string ConnectionEnvironment { get; set; } = "SPO_SQL_PRIMARY";
    public bool CaptureBlocking { get; set; }
    public bool CaptureDeadlocks { get; set; }
    public string XeFilePattern { get; set; } = "system_health*.xel";
}
public sealed class CollectionOptions
{
    public int QueryPeriodSeconds { get; set; } = 120;
    public int BlockingPeriodSeconds { get; set; } = 10;
    public int DeadlockPeriodSeconds { get; set; } = 120;
    public int LookbackHours { get; set; } = 24;
    public int CommandTimeoutSeconds { get; set; } = 5;
    public int QueryRowLimit { get; set; } = 5000;
    public int BlockingRowLimit { get; set; } = 200;
    public int DeadlockLimit { get; set; } = 200;
    public bool AllowUntrustedCertificateForLocalLab { get; set; }
    public void Validate()
    {
        if (QueryPeriodSeconds is < 30 or > 3600 || BlockingPeriodSeconds is < 5 or > 3600
            || DeadlockPeriodSeconds is < 30 or > 3600 || LookbackHours is < 2 or > 48
            || CommandTimeoutSeconds is < 1 or > 30 || QueryRowLimit is < 100 or > 10000
            || BlockingRowLimit is < 10 or > 1000 || DeadlockLimit is < 10 or > 1000)
            throw new InvalidOperationException("Invalid collection settings; see configuration documentation.");
    }
}
