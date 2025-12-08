// ReSharper disable MemberCanBePrivate.Global
namespace ConcreteEngine.Engine.Assets.IO;

internal static class AssetPaths
{
    public const string ManifestFilename = "manifest.json";
    public const string ShaderFolder = "shaders";
    public const string TextureFolder = "textures";
    public const string CubeMapFolder = "cubemaps";
    public const string MeshFolder = "meshes";

    public static readonly string  AssetRoot = Path.Combine(AppContext.BaseDirectory, "assets");
    public static readonly string  AssetCoreRoot = Path.Combine(AppContext.BaseDirectory, "_AssetsCore");
    
    
    public static readonly string ShaderPath =  Path.Combine(AssetRoot, ShaderFolder);
    public static readonly string TexturePath =  Path.Combine(AssetRoot, TextureFolder);
    public static readonly string CubeMapPath =  Path.Combine(AssetRoot, CubeMapFolder);
    public static readonly string MeshPath =  Path.Combine(AssetRoot, MeshFolder);
    
    public static readonly string ShaderCorePath =  Path.Combine(AssetCoreRoot,ShaderFolder, "core-shaders");
    public static readonly string ShaderDefCorePath =  Path.Combine(AssetCoreRoot,ShaderFolder, "definitions");


}