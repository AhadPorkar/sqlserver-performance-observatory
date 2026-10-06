namespace Observatory.Core;
public static class Percentiles
{
    // Only individual lab stopwatch samples may be used here, never Query Store averages.
    public static double NearestRank(IEnumerable<double> samples, double percentile)
    {
        if (!double.IsFinite(percentile) || percentile is <= 0 or > 1) throw new ArgumentOutOfRangeException(nameof(percentile));
        var sorted = samples.Order().ToArray();
        if (sorted.Length == 0 || sorted.Any(x => !double.IsFinite(x) || x < 0)) throw new ArgumentException("Invalid samples");
        return sorted[Math.Max(0, (int)Math.Ceiling(percentile * sorted.Length) - 1)];
    }
}
