// ReSharper disable MemberCanBePrivate.Global

namespace ConcreteEngine.Engine.Configuration.IO;

internal static class EnginePath
{
    public const string ManifestFilename = "manifest.json";

    public const string AssetRoot = "assets";
    public const string ConfigRoot = "config";

    public const string ShaderFolder = "shaders";
    public const string TextureFolder = "textures";
    public const string MeshFolder = "meshes";
    public const string MaterialFolder = "materials";

    // Assets
    public static readonly string ShaderPath = Path.Combine(AssetRoot, ShaderFolder);
    public static readonly string TexturePath = Path.Combine(AssetRoot, TextureFolder);
    public static readonly string MeshPath = Path.Combine(AssetRoot, MeshFolder);
    public static readonly string MaterialPath = Path.Combine(AssetRoot, MaterialFolder);

    // Core Assets
    public static string GetAssetCoreRoot() => Path.Combine(AppContext.BaseDirectory, "_AssetsCore");
    public static readonly string ShaderCorePath = Path.Combine(GetAssetCoreRoot(), ShaderFolder, "core-shaders");
    public static readonly string ShaderDefCorePath = Path.Combine(GetAssetCoreRoot(), ShaderFolder, "definitions");

    // Config
    public static readonly string GraphicSettingsFilePath = Path.Combine(ConfigRoot, "graphics-settings.json");
}