using Observatory.Core;
using Observatory.SqlServer;
namespace Observatory.Web;
public sealed class ObservatoryOptions
{
    public string Mode { get; set; } = "Replay";
    public string StateDirectory { get; set; } = "state";
    public string PublicOrigin { get; set; } = "http://localhost:5080";
    public string[] TrustedProxies { get; set; } = [];
    public int HistoryLimit { get; set; } = 48;
    public CollectionOptions Collection { get; set; } = new();
    public TargetOptions[] Targets { get; set; } = [];
    public void Validate()
    {
        if (Mode is not ("Replay" or "Live")) throw new InvalidOperationException("Mode must be Replay or Live.");
        if (!Uri.TryCreate(PublicOrigin, UriKind.Absolute, out var origin)
            || origin.Scheme is not ("http" or "https") || origin.AbsolutePath != "/"
            || origin.Query.Length > 0 || origin.Fragment.Length > 0 || origin.UserInfo.Length > 0)
            throw new InvalidOperationException("PublicOrigin must contain only scheme, host and optional port.");
        if (origin.Scheme != "https" && !origin.IsLoopback)
            throw new InvalidOperationException("Non-loopback origins require HTTPS.");
        if (HistoryLimit is < 1 or > 288) throw new InvalidOperationException("HistoryLimit must be 1..288.");
        Collection.Validate();
        if (Mode == "Live" && (Targets.Length is < 1 or > 8)) throw new InvalidOperationException("Live mode needs 1..8 configured targets.");
        if (Targets.Select(t => t.Id).Distinct().Count() != Targets.Length) throw new InvalidOperationException("Duplicate target ID.");
        foreach (var t in Targets)
        {
            if (!SnapshotStore.IdPattern().IsMatch(t.Id) || string.IsNullOrWhiteSpace(t.ConnectionEnvironment)
                || t.DisplayName.Length > 100 || t.XeFilePattern.Length is < 1 or > 2048
                || t.XeFilePattern.Contains('\n') || t.XeFilePattern.Contains('\r'))
                throw new InvalidOperationException("Invalid target configuration.");
        }
    }
}
