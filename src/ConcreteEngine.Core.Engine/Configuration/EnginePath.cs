// ReSharper disable MemberCanBePrivate.Global

namespace ConcreteEngine.Core.Engine.Configuration;

public static class EnginePath
{
    public const string AssetRoot = "assets";
    public const string ConfigRoot = "config";

    public const string ShaderFolder = "shaders";
    public const string TextureFolder = "textures";
    public const string MeshFolder = "meshes";
    public const string MaterialFolder = "materials";
    
    public const string ContentFolder = "Content";

    // Assets
    public static readonly string ShaderPath = Path.Join(AssetRoot, ShaderFolder);
    public static readonly string TexturePath = Path.Join(AssetRoot, TextureFolder);
    public static readonly string ModelPath = Path.Join(AssetRoot, MeshFolder);
    public static readonly string MaterialPath = Path.Join(AssetRoot, MaterialFolder);

    // Core Assets
    public static string GetAssetCoreRoot() => Path.Join(AppContext.BaseDirectory, "_AssetsCore");
    public static readonly string ShaderCorePath = Path.Join(GetAssetCoreRoot(), ShaderFolder, "core-shaders");
    public static readonly string ShaderDefCorePath = Path.Join(GetAssetCoreRoot(), ShaderFolder, "definitions");

    // Config
    public static readonly string GraphicSettingsFilePath = Path.Join(ConfigRoot, "graphics-settings.json");
}