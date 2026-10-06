using System.Text.Json;
using Observatory.Core;
namespace Observatory.Tests;
internal static class TestData
{
    public static readonly DateTimeOffset End = DateTimeOffset.Parse("2026-10-05T10:00:00Z");
    public static Snapshot Replay() => JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","replay.json")), JsonDefaults.Create())!;
    public static QueryBucket Bucket(long count, double mean, int hour, long plan = 1)
        => new(42, 1, "0x0000000000000042", plan, "0x0000000000000001", false,
            End.AddHours(hour-1), End.AddHours(hour), count, count*mean, count*mean/2, count*10, mean*2);
    public static Snapshot With(params QueryBucket[] buckets) => Replay() with
    { Buckets = buckets, Mode = "Live", QueryStore = Replay().QueryStore! with { IntervalMinutes=60, LatestClosedEndUtc=End } };
}
