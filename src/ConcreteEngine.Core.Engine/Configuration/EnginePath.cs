// ReSharper disable MemberCanBePrivate.Global

namespace ConcreteEngine.Core.Engine.Configuration;

public static class EnginePath
{
    public const string Root = "AppContent";
    public const string AssetRoot = "assets";
    public const string AssetCoreRoot = "assets-core";

    public const string ConfigRoot = "config";
    public const string DiagnosticRoot = "diagnostics";

    public const string ShaderFolder = "shaders";
    public const string TextureFolder = "textures";
    public const string MeshFolder = "meshes";
    public const string MaterialFolder = "materials";

    public const string EditorContentRoot = "editor";

    // Assets
    public static readonly string AssetBasePath = Path.Join(Root, AssetRoot);
    public static readonly string AssetCoreBasePath = Path.Join(Root, AssetCoreRoot);

    public static readonly string ShaderPath = Path.Join(AssetBasePath, ShaderFolder);
    public static readonly string TexturePath = Path.Join(AssetBasePath, TextureFolder);
    public static readonly string ModelPath = Path.Join(AssetBasePath, MeshFolder);
    public static readonly string MaterialPath = Path.Join(AssetBasePath, MaterialFolder);
    
    public static readonly string ShaderCorePath = Path.Join(AssetCoreBasePath, ShaderFolder);
    public static readonly string ShaderDefPath = Path.Join(AssetCoreBasePath, "shader-definitions");


    // Config
    public static readonly string ConfigPath = Path.Join(Root, ConfigRoot);
    public static readonly string GraphicSettingsFilePath = Path.Join(ConfigPath, "graphics-settings.json");
    
    public static readonly string DiagnosticPath = Path.Join(Root, DiagnosticRoot);
    public static readonly string LoadTimeFilePath = Path.Join(DiagnosticPath, "load-time.txt");
    
    
    public static readonly string EditorContentPath = Path.Join(Root, EditorContentRoot);

}