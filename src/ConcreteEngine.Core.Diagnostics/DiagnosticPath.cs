namespace ConcreteEngine.Core.Diagnostics;

public sealed class DiagnosticPath
{
    public const string Root = "diagnostic";

    public const string PerformanceFile = "perf_last_run.json";
/*
    public static bool TryLoadPerformanceFile(out PerformanceSnapshot snapshot)
    {
        snapshot = default;

        var path = Path.Combine(Root, PerformanceFile);
        if (!File.Exists(path)) return false;
        try
        {
            var json = File.ReadAllText(path);
            snapshot = JsonSerializer.Deserialize<PerformanceSnapshot>(json);
            return true;
        }
        catch
        {
            Console.WriteLine("Failed to load performance snapshot.");
            // ignored
            return false;
        }
    }

    public static bool TrySaveSession(in PerformanceSnapshot snapshot)
    {
        if (!Directory.Exists(Root)) Directory.CreateDirectory(Root);

        try
        {
            var json = JsonSerializer.Serialize(snapshot);
            File.WriteAllText(Path.Combine(Root, PerformanceFile), json);
            return true;
        }
        catch
        {
            Console.WriteLine("Failed to save performance snapshot.");
            return false;
        }
    }*/
}