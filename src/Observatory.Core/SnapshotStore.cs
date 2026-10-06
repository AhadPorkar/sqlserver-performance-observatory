using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Observatory.Core;

// Single-process local persistence. Latest files are atomically replaced; corrupt
// state is reported, not silently interpreted as an empty, healthy target.
public sealed partial class SnapshotStore : IDisposable
{
    private readonly string directory;
    private readonly FileStream lease;
    private readonly int historyLimit;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> gates = new();
    private readonly JsonSerializerOptions json = JsonDefaults.Create();
    public SnapshotStore(string directory, int historyLimit = 48)
    {
        if (historyLimit is < 1 or > 288) throw new ArgumentOutOfRangeException(nameof(historyLimit));
        this.directory = Path.GetFullPath(directory);
        this.historyLimit = historyLimit;
        Directory.CreateDirectory(this.directory);
        lease = new FileStream(Path.Combine(this.directory, ".writer.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
    public async Task<Snapshot?> LatestAsync(string target, CancellationToken ct = default)
    {
        var dir = TargetDirectory(target);
        var file = Path.Combine(dir, "latest.json");
        if (!File.Exists(file)) return null;
        await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        if (stream.Length > 32 * 1024 * 1024) throw new InvalidDataException("STATE_TOO_LARGE");
        var value = await JsonSerializer.DeserializeAsync<Snapshot>(stream, json, ct);
        if (value is null || value.SchemaVersion != 1 || value.TargetId != target) throw new InvalidDataException("STATE_SCHEMA_MISMATCH");
        return value;
    }
    public async Task SaveAsync(Snapshot snapshot, bool archive, CancellationToken ct = default)
    {
        var dir = TargetDirectory(snapshot.TargetId);
        var gate = gates.GetOrAdd(snapshot.TargetId, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(ct);
        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(snapshot, json);
            if (payload.Length > 32 * 1024 * 1024) throw new InvalidDataException("STATE_TOO_LARGE");
            await AtomicAsync(Path.Combine(dir, "latest.json"), payload, ct);
            if (archive)
            {
                var history = Path.Combine(dir, "history"); Directory.CreateDirectory(history);
                // Snapshot IDs are generated internally. Never accept filenames from HTTP.
                await AtomicAsync(Path.Combine(history, $"{snapshot.CapturedUtc:yyyyMMddTHHmmssfffffff}_{Guid.NewGuid():N}.json"), payload, ct);
                foreach (var old in Directory.EnumerateFiles(history, "*.json").OrderDescending().Skip(historyLimit)) File.Delete(old);
            }
        }
        finally { gate.Release(); }
    }
    private static async Task AtomicAsync(string file, byte[] bytes, CancellationToken ct)
    {
        var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.WriteThrough))
            { await stream.WriteAsync(bytes, ct); await stream.FlushAsync(ct); stream.Flush(true); }
            File.Move(temp, file, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private string TargetDirectory(string target)
    {
        if (!IdPattern().IsMatch(target)) throw new ArgumentException("Invalid target ID", nameof(target));
        var path = Path.Combine(directory, target); Directory.CreateDirectory(path); return path;
    }
    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,47}$", RegexOptions.CultureInvariant)]
    public static partial Regex IdPattern();
    public void Dispose() { lease.Dispose(); foreach (var gate in gates.Values) gate.Dispose(); }
}
