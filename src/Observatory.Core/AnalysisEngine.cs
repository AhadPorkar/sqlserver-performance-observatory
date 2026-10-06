namespace Observatory.Core;

public static class AnalysisEngine
{
    public static AnalysisReport Analyze(Snapshot snapshot, AnalysisSettings settings)
    {
        if (settings.BaselineMinutes is < 1 or > 1440 || settings.RecentMinutes is < 1 or > 1440
            || settings.MinimumExecutions < 1 || !double.IsFinite(settings.MinimumRatio)
            || settings.MinimumRatio < 1 || !double.IsFinite(settings.MinimumDeltaMs)
            || settings.MinimumDeltaMs < 0 || !double.IsFinite(settings.MinimumBaselineMs)
            || settings.MinimumBaselineMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(settings));

        var warnings = new List<string>();
        var end = snapshot.QueryStore?.LatestClosedEndUtc;
        var enabled = snapshot.QueriesStatus.State == "ok" && !snapshot.QueriesStatus.Truncated;
        if (!enabled) warnings.Add("QUERY_SOURCE_INCOMPLETE");
        if (snapshot.Mode != "Live") warnings.Add("SYNTHETIC_FIXTURE_NOT_A_BENCHMARK");
        if (snapshot.QueryStore?.State is not ("READ_WRITE" or "READ_CAPTURE_SECONDARY"))
            warnings.Add("QUERY_STORE_NOT_CAPTURING_NORMAL_PRIMARY_WORKLOAD");
        if (end is null)
            return new(null, null, null, settings.BaselineMinutes, settings.RecentMinutes,
                [], [.. warnings, "NO_CLOSED_INTERVALS"], false);
        var split = end.Value.AddMinutes(-settings.RecentMinutes);
        var start = split.AddMinutes(-settings.BaselineMinutes);
        var selected = snapshot.Buckets.Where(Valid).Where(x => x.EndUtc <= end.Value && x.StartUtc >= start).ToArray();
        // A Query Store interval is indivisible. Never prorate its executions.
        if (snapshot.Buckets.Any(x => x.StartUtc < split && x.EndUtc > split))
            warnings.Add("BOUNDARY_CROSSING_INTERVALS_EXCLUDED");
        if (snapshot.QueryStore is { } info && (settings.BaselineMinutes % Math.Max(1, info.IntervalMinutes) != 0
            || settings.RecentMinutes % Math.Max(1, info.IntervalMinutes) != 0))
            warnings.Add("WINDOW_NOT_MULTIPLE_OF_CAPTURE_INTERVAL");
        if (selected.Length == 0) warnings.Add("NO_MATCHING_SUCCESSFUL_EXECUTIONS");
        var reports = selected.GroupBy(x => (x.QueryId, x.ContextId, x.QueryHash)).Select(g =>
        {
            var before = Summarize(g.Where(x => x.EndUtc <= split));
            var after = Summarize(g.Where(x => x.StartUtc >= split));
            var codes = new List<string>();
            var sufficient = before.Executions >= settings.MinimumExecutions && after.Executions >= settings.MinimumExecutions;
            if (!sufficient) codes.Add("INSUFFICIENT_EXECUTIONS");
            var baseUsable = before.MeanMs >= settings.MinimumBaselineMs;
            if (!baseUsable) codes.Add("BASELINE_TOO_SMALL_OR_MISSING");
            double? ratio = baseUsable && after.MeanMs.HasValue ? after.MeanMs.Value / before.MeanMs!.Value : null;
            double? delta = before.MeanMs.HasValue && after.MeanMs.HasValue ? after.MeanMs.Value - before.MeanMs.Value : null;
            bool changed = before.Plans.Length > 0 && after.Plans.Length > 0 && !before.Plans.SequenceEqual(after.Plans);
            if (changed) codes.Add("PLAN_SET_CHANGED_CORRELATION_ONLY");
            var candidate = enabled && sufficient && baseUsable && ratio >= settings.MinimumRatio && delta >= settings.MinimumDeltaMs;
            var classification = !enabled ? "incomplete" : !sufficient || !baseUsable ? "insufficient" : candidate ? "candidate" : "stable";
            if (candidate) codes.Add("WEIGHTED_DURATION_THRESHOLD_EXCEEDED");
            return new QueryComparison(g.Key.QueryId, g.Key.QueryHash, g.Key.ContextId,
                before, after, ratio, delta, delta.HasValue ? Math.Max(0, delta.Value) * after.Executions : null,
                changed, classification, codes.ToArray());
        }).OrderByDescending(x => x.Classification == "candidate")
          .ThenByDescending(x => x.EstimatedExtraMs).ThenBy(x => x.QueryId).ToArray();
        warnings.Add("AGGREGATED_SUCCESSFUL_EXECUTIONS_ONLY_NO_PERCENTILES");
        return new(start, split, end, settings.BaselineMinutes, settings.RecentMinutes, reports, warnings.ToArray(), enabled);
    }

    public static WindowStats Summarize(IEnumerable<QueryBucket> buckets)
    {
        var rows = buckets.Where(Valid).ToArray();
        long count = rows.Sum(x => x.Executions);
        double duration = rows.Sum(x => x.TotalDurationMs);
        return new(count, count == 0 ? null : duration / count, duration,
            count == 0 ? null : rows.Sum(x => x.TotalCpuMs) / count,
            count == 0 ? null : rows.Sum(x => x.TotalLogicalReads) / count,
            rows.Length == 0 ? 0 : rows.Max(x => x.MaxDurationMs),
            rows.Select(x => x.PlanId).Distinct().Order().ToArray());
    }
    public static bool Valid(QueryBucket b) => b.Executions > 0 && b.StartUtc < b.EndUtc
        && double.IsFinite(b.TotalDurationMs) && b.TotalDurationMs >= 0
        && double.IsFinite(b.TotalCpuMs) && b.TotalCpuMs >= 0
        && double.IsFinite(b.TotalLogicalReads) && b.TotalLogicalReads >= 0
        && double.IsFinite(b.MaxDurationMs) && b.MaxDurationMs >= 0;
}
