namespace Observatory.SqlServer;
public static class SqlQueries
{
    public static string Load(string name)
    {
        if (name is not ("Capabilities" or "QueryBuckets" or "Blocking" or "Deadlocks"))
            throw new ArgumentException("Not an approved read query", nameof(name));
        using var stream = typeof(SqlQueries).Assembly.GetManifestResourceStream($"Observatory.SqlServer.Queries.{name}.sql")
            ?? throw new InvalidOperationException("Missing embedded SQL resource");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
