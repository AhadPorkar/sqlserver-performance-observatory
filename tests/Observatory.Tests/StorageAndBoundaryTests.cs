using System.Text.Json;
using Observatory.Core;
using Observatory.SqlServer;
using Xunit;
namespace Observatory.Tests;
public sealed class StorageAndBoundaryTests
{
    [Fact]public void BigintSurvivesBrowserWireFormat()
    {
        long id=9007199254740993;var json=JsonSerializer.Serialize(id,JsonDefaults.Create());
        Assert.Equal("\"9007199254740993\"",json);Assert.Equal(id,JsonSerializer.Deserialize<long>(json,JsonDefaults.Create()));
    }
    [Fact]public async Task StoreRoundTripAndSingleWriterLease()
    {
        var dir=Path.Combine(Path.GetTempPath(),"spo-"+Guid.NewGuid().ToString("N"));
        try
        {
            using var store=new SnapshotStore(dir,2);var s=TestData.Replay();await store.SaveAsync(s,true);
            Assert.Equal(s.Id,(await store.LatestAsync(s.TargetId))!.Id);
            Assert.Throws<IOException>(()=>new SnapshotStore(dir));
            await store.SaveAsync(s with{Id="next"},true);await store.SaveAsync(s with{Id="last"},true);
            Assert.Equal(2,Directory.GetFiles(Path.Combine(dir,"demo","history")).Length);
            Assert.Equal("last",(await store.LatestAsync("demo"))!.Id);
        }
        finally{Directory.Delete(dir,true);}
    }
    [Theory][InlineData("../x")][InlineData("/tmp")][InlineData("bad/id")][InlineData("bad\\id")][InlineData("UPPER")]
    public void TargetIdsCannotTraverse(string id)=>Assert.False(SnapshotStore.IdPattern().IsMatch(id));
    [Fact]public void UnknownEmbeddedSqlRejected()=>Assert.Throws<ArgumentException>(()=>SqlQueries.Load("DropDatabase"));
    [Theory][InlineData("Capabilities")][InlineData("QueryBuckets")][InlineData("Blocking")][InlineData("Deadlocks")]
    public void ReadQueriesAreEmbedded(string name)=>Assert.Contains("SELECT",SqlQueries.Load(name));
    [Fact]public void QueryStoreDoesNotCollectText()=>Assert.DoesNotContain("query_sql_text",SqlQueries.Load("QueryBuckets"));
    [Fact]public void QueryUsesClosedIntervalsAndWeights()
    {
        var sql=SqlQueries.Load("QueryBuckets");Assert.Contains("rs.execution_type = 0",sql);
        Assert.Contains("i.end_time <= @ToUtc",sql);Assert.Contains("rs.count_executions) * rs.avg_duration",sql);
    }
    [Fact]public void RemoteTrustBypassIsRefused()
    {
        string key="SPO_TEST_"+Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(key,"Server=remote.example.test;Database=ObservatoryLab;User Id=reader;Password=not-a-real-secret;TrustServerCertificate=True");
        try{Assert.Throws<InvalidOperationException>(()=>new SqlConnectionFactory(new(){AllowUntrustedCertificateForLocalLab=true}).Create(new(){ConnectionEnvironment=key}));}
        finally{Environment.SetEnvironmentVariable(key,null);}
    }
    [Fact]public void SaConnectionIsRefused()
    {
        string key="SPO_TEST_"+Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(key,"Server=localhost;Database=ObservatoryLab;User Id=sa;Password=not-a-real-secret");
        try{Assert.Throws<InvalidOperationException>(()=>new SqlConnectionFactory(new()).Create(new(){ConnectionEnvironment=key}));}
        finally{Environment.SetEnvironmentVariable(key,null);}
    }
}
