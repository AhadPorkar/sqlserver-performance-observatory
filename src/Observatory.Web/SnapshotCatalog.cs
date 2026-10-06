using System.Collections.Concurrent;
using Observatory.Core;
namespace Observatory.Web;
public sealed class SnapshotCatalog
{
    private readonly ConcurrentDictionary<string, Snapshot> snapshots = new();
    public Snapshot? Get(string id) => snapshots.GetValueOrDefault(id);
    public Snapshot[] All() => snapshots.Values.OrderBy(x => x.TargetId).ToArray();
    public void Put(Snapshot snapshot) => snapshots[snapshot.TargetId] = snapshot;
}
