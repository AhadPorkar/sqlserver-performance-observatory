namespace Observatory.Core;

public static class BlockingAnalyzer
{
    public static BlockingChain[] Build(IEnumerable<BlockingRow> input)
    {
        var rows = input.GroupBy(x => x.SessionId).ToDictionary(g => g.Key, g => g.First());
        return rows.Values.OrderByDescending(x => x.WaitMs).Select(row =>
        {
            var path = new List<int> { row.SessionId };
            var seen = new HashSet<int> { row.SessionId };
            var current = row;
            for (int depth = 0; depth < 64; depth++)
            {
                var next = current.BlockerId;
                if (next == 0) return new BlockingChain(row.SessionId, path.ToArray(), "not-blocked", "", null, row.WaitMs);
                path.Add(next);
                if (next < 0) return new(row.SessionId, path.ToArray(), $"special-owner:{next}", "not-a-session", null, row.WaitMs);
                if (!seen.Add(next)) return new(row.SessionId, path.ToArray(), "cycle-observed", "", null, row.WaitMs);
                if (rows.TryGetValue(next, out var parent)) { current = parent; continue; }
                var known = current.BlockerStatus.Length > 0;
                var terminal = !known || current.BlockerNextId is > 0 or < 0 ? "outside-sample" : "root-observed";
                return new(row.SessionId, path.ToArray(), terminal, current.BlockerStatus, current.BlockerOpenTransactions, row.WaitMs);
            }
            return new(row.SessionId, path.ToArray(), "depth-limit", "", null, row.WaitMs);
        }).ToArray();
    }
}
