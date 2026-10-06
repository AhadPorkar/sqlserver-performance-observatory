using Observatory.Core;
using Xunit;
namespace Observatory.Tests;
public sealed class AnalysisTests
{
    [Fact] public void WeightedMeanIsNotAnAverageOfAverages()
    {
        var value=AnalysisEngine.Summarize([TestData.Bucket(100,10,0),TestData.Bucket(1,1000,0)]);
        Assert.Equal(101,value.Executions); Assert.Equal(2000.0/101,value.MeanMs!.Value,8);
    }
    [Fact] public void CountsAndPlanSetProduceAQualifiedCandidate()
    {
        var a=AnalysisEngine.Analyze(TestData.With(TestData.Bucket(30,10,-1),TestData.Bucket(20,100,0,2)),new());
        var q=Assert.Single(a.Queries); Assert.Equal("candidate",q.Classification);
        Assert.True(q.PlanSetChanged); Assert.Equal(10d,q.Ratio!.Value); Assert.Equal(1800d,q.EstimatedExtraMs!.Value);
    }
    [Fact] public void IdenticalPlanDoesNotSuppressARealDurationSignal()
    {
        var q=Assert.Single(AnalysisEngine.Analyze(TestData.With(TestData.Bucket(20,10,-1),TestData.Bucket(20,100,0)),new()).Queries);
        Assert.Equal("candidate",q.Classification);Assert.False(q.PlanSetChanged);
    }
    [Fact] public void TruncatedSourceSuppressesClassification()
    {
        var s=TestData.With(TestData.Bucket(30,10,-1),TestData.Bucket(30,100,0));
        s=s with{QueriesStatus=s.QueriesStatus with{Truncated=true}};
        var a=AnalysisEngine.Analyze(s,new());Assert.False(a.ComparisonsEnabled);Assert.Equal("incomplete",Assert.Single(a.Queries).Classification);
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(9)]
    public void SmallSampleCannotBecomeCandidate(int n)
    {
        var q=Assert.Single(AnalysisEngine.Analyze(TestData.With(TestData.Bucket(30,10,-1),TestData.Bucket(n,100,0)),new()).Queries);
        Assert.Equal("insufficient",q.Classification);
    }
    [Fact] public void NearZeroBaselineDoesNotProduceInfinity()
    {
        var q=Assert.Single(AnalysisEngine.Analyze(TestData.With(TestData.Bucket(30,0,-1),TestData.Bucket(30,100,0)),new()).Queries);
        Assert.Null(q.Ratio);Assert.Equal("insufficient",q.Classification);
    }
    [Fact] public void CrossingIntervalIsNotProrated()
    {
        var b=TestData.Bucket(300,100,0) with{StartUtc=TestData.End.AddMinutes(-90),EndUtc=TestData.End.AddMinutes(-30)};
        var a=AnalysisEngine.Analyze(TestData.With(b),new());
        var q=Assert.Single(a.Queries);Assert.Equal(0,q.Baseline.Executions);Assert.Equal(0,q.Recent.Executions);
        Assert.Contains("BOUNDARY_CROSSING_INTERVALS_EXCLUDED",a.Warnings);
    }
    [Fact] public void OpenIntervalIsExcluded()
    {
        var b=TestData.Bucket(100,900,0) with{EndUtc=TestData.End.AddMinutes(10)};
        Assert.Empty(AnalysisEngine.Analyze(TestData.With(b),new()).Queries);
    }
    [Fact] public void ReanalyzingSnapshotDoesNotAccumulateCounters()
    {
        var s=TestData.Replay();var a=AnalysisEngine.Analyze(s,new());var b=AnalysisEngine.Analyze(s,new());
        Assert.Equal(a.Queries.Select(q=>q.Recent.Executions),b.Queries.Select(q=>q.Recent.Executions));
    }
    [Fact] public void ReplayHasTwoIllustrativeCandidates()
    {
        var a=AnalysisEngine.Analyze(TestData.Replay(),new());Assert.Equal(2,a.Queries.Count(q=>q.Classification=="candidate"));
        Assert.Contains("SYNTHETIC_FIXTURE_NOT_A_BENCHMARK",a.Warnings);
    }
    [Fact] public void NoClosedIntervalIsAnExplicitUnknown()
    {
        var s=TestData.Replay();s=s with{QueryStore=s.QueryStore! with{LatestClosedEndUtc=null}};
        var a=AnalysisEngine.Analyze(s,new());Assert.False(a.ComparisonsEnabled);Assert.Contains("NO_CLOSED_INTERVALS",a.Warnings);
    }
    [Theory][InlineData(-1)][InlineData(0)][InlineData(1441)]
    public void InvalidWindowRejected(int m)=>Assert.Throws<ArgumentOutOfRangeException>(()=>AnalysisEngine.Analyze(TestData.Replay(),new(m)));
    [Fact] public void CpuAndReadsAreAlsoWeighted()
    {
        var a=TestData.Bucket(20,10,0) with{TotalCpuMs=80,TotalLogicalReads=500};
        var b=TestData.Bucket(5,40,0) with{TotalCpuMs=50,TotalLogicalReads=100};
        var stats=AnalysisEngine.Summarize([a,b]);Assert.Equal(5.2,stats.MeanCpuMs!.Value);Assert.Equal(24d,stats.MeanReads!.Value);
    }
    [Fact] public void NegativeAndNonFiniteCountersAreInvalid()
    {
        Assert.False(AnalysisEngine.Valid(TestData.Bucket(1,1,0) with{TotalDurationMs=double.NaN}));
        Assert.False(AnalysisEngine.Valid(TestData.Bucket(1,1,0) with{TotalCpuMs=-1}));
    }
    [Fact] public void PercentilesOnlyUseIndividualSamples()
    {
        Assert.Equal(95,Percentiles.NearestRank(Enumerable.Range(1,100).Select(x=>(double)x),.95));
        Assert.Equal(50,Percentiles.NearestRank(Enumerable.Range(1,100).Select(x=>(double)x),.5));
        Assert.Throws<ArgumentException>(()=>Percentiles.NearestRank([], .95));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Percentiles.NearestRank([1,2], double.NaN));
    }
}
