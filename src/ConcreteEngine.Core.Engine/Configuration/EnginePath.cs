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

    public const string EditorContentRoot = "editor";

    // Assets
    public const int AssetBasePathOffset = 17;
    public const string AssetBasePath = Root + "/" + AssetRoot;
    public const string AssetCoreBasePath = Root + "/" + AssetCoreRoot;
    
    public static readonly string ShaderCorePath = Path.Join(AssetCoreBasePath, ShaderFolder);
    public static readonly string ShaderDefPath = Path.Join(AssetCoreBasePath, "shader-definitions");

    // Config
    public static readonly string ConfigPath = Path.Join(Root, ConfigRoot);
    public static readonly string GraphicSettingsFilePath = Path.Join(ConfigPath, "graphics-settings.json");

    public static readonly string DiagnosticPath = Path.Join(Root, DiagnosticRoot);
    public static readonly string LoadTimeFilePath = Path.Join(DiagnosticPath, "load-time.txt");


    public static readonly string EditorContentPath = Path.Join(Root, EditorContentRoot);
}