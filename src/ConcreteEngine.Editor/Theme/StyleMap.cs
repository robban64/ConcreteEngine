using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Theme;

internal static class StyleMap
{
    private static readonly NativeArray<Color4> SceneColors =
        NativeArray.Allocate<Color4>(int.Max(EnumCache<SceneObjectKind>.Count, 4));

    private static readonly NativeArray<Color4> AssetColors =
        NativeArray.Allocate<Color4>(int.Max(EnumCache<AssetKind>.Count, 4));

    private static readonly NativeArray<Color4> LogLevelColors = NativeArray.Allocate<Color4>(7);

    public static ref readonly Color4 GetSceneColor(SceneObjectKind kind) => ref SceneColors[(int)kind];
    public static ref readonly Color4 GetAssetColor(AssetKind kind) => ref AssetColors[(int)kind];
    public static ref readonly Color4 GetLogLevelColor(LogLevel level) => ref LogLevelColors[(int)level];

    internal static void Init()
    {
        SceneColors[(int)SceneObjectKind.Empty] = Palette.GrayLight;
        SceneColors[(int)SceneObjectKind.Model] = Palette.Model;
        SceneColors[(int)SceneObjectKind.Particle] = Palette.CyanLight;

        AssetColors[(int)AssetKind.Unknown] = Palette.GrayLight;
        AssetColors[(int)AssetKind.Shader] = Palette.Shader;
        AssetColors[(int)AssetKind.Model] = Palette.Model;
        AssetColors[(int)AssetKind.Texture] = Palette.Texture;
        AssetColors[(int)AssetKind.Material] = Palette.Material;

        LogLevelColors[(int)LogLevel.None] = Color4.White;
        LogLevelColors[(int)LogLevel.Trace] = Palette.GrayLight;
        LogLevelColors[(int)LogLevel.Debug] = Palette.BlueLight;
        LogLevelColors[(int)LogLevel.Info] = Palette.GreenBase;
        LogLevelColors[(int)LogLevel.Warn] = Palette.OrangeBase;
        LogLevelColors[(int)LogLevel.Error] = Palette.RedBase;
        LogLevelColors[(int)LogLevel.Critical] = Palette.RedLight;
    }
}