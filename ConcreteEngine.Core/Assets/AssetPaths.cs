namespace ConcreteEngine.Core.Assets;

internal static class AssetPaths
{
    public static string AssetFolder { get; internal set; } = null!;
    public static string AssetCoreFolder => "_AssetsCore";

    public static string GetAssetPath() => Path.Combine(Directory.GetCurrentDirectory(), AssetFolder);
    public static string GetAssetCorePath() => Path.Combine(Directory.GetCurrentDirectory(), AssetCoreFolder);

}