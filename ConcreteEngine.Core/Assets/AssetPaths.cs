namespace ConcreteEngine.Core.Assets;

internal static class AssetPaths
{
    public static string AssetFolder { get; internal set; } = null!;
    public static string AssetCoreFolder => "_AssetsCore";

    public static string GetAssetPath() => Path.Combine(Directory.GetCurrentDirectory(), AssetFolder);
    public static string GetAssetCorePath() => Path.Combine(Directory.GetCurrentDirectory(), AssetCoreFolder);

    public static string ShaderFolder { get; internal set; }= null!;
    public static string TextureFolder { get; internal set; }= null!;
    public static string MeshFolder { get; internal set; }= null!;
    public static string Material { get; internal set; }= null!;
    public static string CubeMaps { get; internal set; }= null!;

}