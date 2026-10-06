using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
namespace Observatory.Core;

public static class DeadlockParser
{
    public const int MaxXmlCharacters = 1_000_000;
    public static DeadlockEvent? Parse(string xml, DateTimeOffset timestampUtc, int targetDatabaseId)
    {
        if (xml.Length > MaxXmlCharacters) throw new InvalidDataException("XE_XML_TOO_LARGE");
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null, MaxCharactersInDocument = MaxXmlCharacters };
        using var input = new StringReader(xml);
        using var reader = XmlReader.Create(input, settings);
        var document = XDocument.Load(reader, LoadOptions.None);
        var deadlock = document.Root?.Name.LocalName == "deadlock" ? document.Root
            : document.Descendants().FirstOrDefault(x => x.Name.LocalName == "deadlock");
        if (deadlock is null) return null;
        var victims = deadlock.Descendants("victimProcess").Select(x => SafeId((string?)x.Attribute("id"))).ToHashSet();
        var processes = deadlock.Element("process-list")?.Elements("process").Select(x =>
            new DeadlockProcess(SafeId((string?)x.Attribute("id")), Number(x, "spid"), Number(x, "currentdb"),
                victims.Contains(SafeId((string?)x.Attribute("id"))))).Take(129).ToArray() ?? [];
        if (processes.Length > 128) throw new InvalidDataException("XE_TOO_MANY_PROCESSES");
        if (!processes.Any(x => x.DatabaseId == targetDatabaseId)) return null;
        var resources = deadlock.Element("resource-list")?.Elements().Select((x, i) =>
            new DeadlockResource($"r{i}", Kind(x.Name.LocalName),
                Participants(x.Element("owner-list"), "owner"), Participants(x.Element("waiter-list"), "waiter")))
            .Take(257).ToArray() ?? [];
        if (resources.Length > 256) throw new InvalidDataException("XE_TOO_MANY_RESOURCES");
        // No SQL, parameter, login, host, object or index name is retained.
        var normalized = deadlock.ToString(SaveOptions.DisableFormatting);
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(timestampUtc.ToUniversalTime().ToString("O") + normalized))).ToLowerInvariant();
        return new(id, timestampUtc.ToUniversalTime(), processes, resources);
    }
    private static LockParticipant[] Participants(XElement? root, string kind)
    {
        var items = root?.Elements(kind).Select(x => new LockParticipant(SafeId((string?)x.Attribute("id")), Mode((string?)x.Attribute("mode")))).Take(129).ToArray() ?? [];
        if (items.Length > 128) throw new InvalidDataException("XE_TOO_MANY_PARTICIPANTS");
        return items;
    }
    private static string SafeId(string? value) => new((value ?? "unknown").Where(char.IsAsciiLetterOrDigit).Take(64).ToArray());
    private static int Number(XElement x, string name) => int.TryParse((string?)x.Attribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
    private static string Mode(string? mode) => mode is "S" or "U" or "X" or "IS" or "IX" or "SIX" or "Sch-S" or "Sch-M" or "RangeS-S" or "RangeS-U" or "RangeI-N" or "RangeX-X" ? mode : "other";
    private static string Kind(string kind) => kind is "keylock" or "pagelock" or "objectlock" or "ridlock" or "exchangeEvent" or "threadpool" or "metadatalock" or "applicationlock" ? kind : "other";
}
