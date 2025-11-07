namespace ConcreteEngine.Engine.Assets.IO;

internal static class AssetPaths
{
    public const string AssetCoreFolder = "_AssetsCore";
    public const string AssetFolder = "assets";

    public const string ManifestFilename = "manifest.json";
    public const string ShaderFolder = "shaders";
    public const string TextureFolder = "textures";
    public const string CubeMapFolder = "cubemaps";
    public const string MeshFolder = "meshes";

    public const string DefinitionFolder = "definitions";

    public static string AssetPath => Path.Combine(AppContext.BaseDirectory, AssetFolder);

    public static string GetAssetSubPath(string p) => Path.Combine(AssetPath, p);
    public static string GetAssetSubPath(string p1, string p2) => Path.Combine(AssetPath, p1, p2);

    public static string GetManifestPath() => GetAssetSubPath(ManifestFilename);
    public static string GetShaderPath(string fileName) => GetAssetSubPath(ShaderFolder, fileName);
    public static string GetTexturePath(string fileName) => GetAssetSubPath(TextureFolder, fileName);
    public static string GetCubeMapPath(string fileName) => GetAssetSubPath(CubeMapFolder, fileName);
    public static string GetMeshPath(string fileName) => GetAssetSubPath(MeshFolder, fileName);


    internal static string CorePath => Path.Combine(AppContext.BaseDirectory, AssetCoreFolder);

    internal static string CoreShaderPath(string subPath, string fileName) =>
        Path.Combine(CorePath, ShaderFolder, subPath, fileName);

    internal static string CoreTexturePath(string fileName) => Path.Combine(CorePath, TextureFolder, fileName);
    internal static string CoreCubeMapPath(string fileName) => Path.Combine(CorePath, CubeMapFolder, fileName);
    internal static string CoreMeshPath(string fileName) => Path.Combine(CorePath, MeshFolder, fileName);
}