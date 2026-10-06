using System.Text.Json;
using System.Xml;
using Observatory.Core;
using Xunit;
namespace Observatory.Tests;
public sealed class BlockingAndXmlTests
{
    private static BlockingRow Row(int id,int blocker,string root="sleeping",int? next=null)=>new(id,blocker,"suspended","LCK_M_X",2000,3000,1,root,next,1);
    [Fact]public void SleepingRootIsNotLost()
    {
        var chains=BlockingAnalyzer.Build([Row(81,63,"suspended",55),Row(63,55)]);
        var chain=chains.Single(x=>x.SessionId==81);Assert.Equal(new[]{81,63,55},chain.Path);
        Assert.Equal("root-observed",chain.Terminal);Assert.Equal("sleeping",chain.RootStatus);
    }
    [Fact]public void CyclesTerminate()=>Assert.All(BlockingAnalyzer.Build([Row(1,2),Row(2,1)]),x=>Assert.Equal("cycle-observed",x.Terminal));
    [Theory][InlineData(-2)][InlineData(-3)][InlineData(-4)][InlineData(-5)]
    public void SpecialOwnersAreNotSessionRoots(int id)=>Assert.Equal($"special-owner:{id}",Assert.Single(BlockingAnalyzer.Build([Row(1,id)])).Terminal);
    [Fact]public void OutsideDatabaseChainIsIncomplete()=>Assert.Equal("outside-sample",Assert.Single(BlockingAnalyzer.Build([Row(1,2,"suspended",3)])).Terminal);
    [Fact]public void UnknownRootIsExplicit()=>Assert.Equal("outside-sample",Assert.Single(BlockingAnalyzer.Build([Row(1,2,"")])).Terminal);
    private static string Xml()=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","deadlock.xml"));
    [Fact]public void DeadlockSummaryKeepsGraphAndOmitsSensitiveText()
    {
        var d=DeadlockParser.Parse(Xml(),TestData.End,7)!;Assert.Equal(2,d.Processes.Length);Assert.Equal(2,d.Resources.Length);
        Assert.Single(d.Processes.Where(p=>p.Victim));var json=JsonSerializer.Serialize(d,JsonDefaults.Create());
        Assert.DoesNotContain("DO_NOT_PERSIST",json);Assert.DoesNotContain("never-export",json);
        Assert.Equal("processA",d.Resources[0].Owners[0].ProcessId);
    }
    [Fact]public void OtherDatabasesAreFiltered()=>Assert.Null(DeadlockParser.Parse(Xml(),TestData.End,99));
    [Fact]public void FingerprintIsStable()=>Assert.Equal(DeadlockParser.Parse(Xml(),TestData.End,7)!.Id,DeadlockParser.Parse(Xml(),TestData.End,7)!.Id);
    [Fact]public void DifferentTimestampIsADifferentEvent()=>Assert.NotEqual(DeadlockParser.Parse(Xml(),TestData.End,7)!.Id,DeadlockParser.Parse(Xml(),TestData.End.AddSeconds(1),7)!.Id);
    [Fact]public void ExternalEntitiesAreRejected()=>Assert.Throws<XmlException>(()=>DeadlockParser.Parse("<!DOCTYPE deadlock [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><deadlock>&x;</deadlock>",TestData.End,7));
    [Fact]public void OversizedXmlIsRejected()=>Assert.Throws<InvalidDataException>(()=>DeadlockParser.Parse(new string('x',DeadlockParser.MaxXmlCharacters+1),TestData.End,7));
    [Fact]public void MalformedXmlIsRejected()=>Assert.Throws<XmlException>(()=>DeadlockParser.Parse("<deadlock>",TestData.End,7));
    [Fact]public void NonDeadlockIsIgnored()=>Assert.Null(DeadlockParser.Parse("<event name='other'/>",TestData.End,7));
}
