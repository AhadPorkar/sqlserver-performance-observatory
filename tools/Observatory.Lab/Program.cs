using System.Text.Json;
using Observatory.Core;
using Observatory.Lab;

if (args.Length == 0 || !args.Contains("--accept-lab-only") || args[0] is not ("setup" or "benchmark" or "blocking" or "deadlock" or "verify-reader"))
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/Observatory.Lab -- <setup|benchmark|blocking|deadlock|verify-reader> --accept-lab-only [--align-intervals]");
    Console.Error.WriteLine("This tool MUTATES a dedicated LOCAL SQL Server Developer database named ObservatoryLab. Never point it at production.");
    return 2;
}
if (args.Skip(1).Any(a => a is not ("--accept-lab-only" or "--align-intervals"))) return 2;
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(12));
try
{
    var lab = new LabEnvironment(); object? report = null;
    switch (args[0])
    {
        case "setup": await lab.SetupAsync(timeout.Token); break;
        case "benchmark": report = await new Workload(lab).BenchmarkAsync(args.Contains("--align-intervals"), timeout.Token); break;
        case "blocking": report = await new ConcurrencyDemos(lab).BlockingAsync(timeout.Token); break;
        case "deadlock": report = await new ConcurrencyDemos(lab).DeadlockAsync(timeout.Token); break;
        case "verify-reader": report = await new ConcurrencyDemos(lab).VerifyReaderAsync(timeout.Token); break;
    }
    if (report is not null)
    {
        Directory.CreateDirectory("artifacts");
        var path = Path.Combine("artifacts", $"lab-{args[0]}-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfff}.json");
        var options = JsonDefaults.Create(); options.WriteIndented = true;
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, options), timeout.Token);
        Console.WriteLine("Evidence written: " + path);
    }
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Lab failed ({ex.GetType().Name}): {ex.Message}");
    return 1;
}
