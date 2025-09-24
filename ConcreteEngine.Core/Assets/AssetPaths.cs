namespace ConcreteEngine.Core.Assets;

public static class AssetPaths
{
    public static readonly string RootPath = Directory.GetCurrentDirectory();
    public static string AssetPath { get; internal set; } = null!;

    public static string GetAbsolutePath() => Path.Combine(RootPath, AssetPath);
}