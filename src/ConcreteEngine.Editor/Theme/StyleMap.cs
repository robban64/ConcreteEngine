using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Utils;
using static ConcreteEngine.Editor.Utils.IconNames;

namespace ConcreteEngine.Editor.Theme;

public enum Icons : byte
{
    Activity, LayoutGrid, Play, Pause,
    Move3d, Scale3d, Rotate3d,
    MousePointer2, Sun, CloudFog, Sparkles,
    Undo2, Eye, EyeClosed, Code, Image, Video,
    Cuboid, Box, Boxes, Circle, CircleDashed,
}

internal static unsafe class StyleMap
{
    private static readonly NativeArray<Color4> ColorBuffer = NativeArray.Allocate<Color4>(16);
    private static readonly NativeArray<byte> IconBuffer = NativeArray.Allocate<byte>(128);

    private static Color4* _assetColorPtr;
    private static Color4* _logLevelPtr;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* GetIcon(Icons icon) => IconBuffer + ((int)icon * 4);

    public static ref readonly Color4 GetSceneColor(SceneObjectKind kind) => ref ColorBuffer[(int)kind];
    public static ref readonly Color4 GetAssetColor(AssetKind kind) => ref _assetColorPtr[(int)kind];
    public static ref readonly Color4 GetLogLevelColor(LogLevel level) => ref _logLevelPtr[(int)level];

    internal static void Init()
    {
        InitIcons();
        InitColors();
    }

    private static void InitIcons()
    {
        Span<char> icons =
        [
            Activity, LayoutGrid, Play, Pause,
            Move3d, Scale3d, Rotate3d,
            MousePointer2, Sun, CloudFog, Sparkles,
            Undo2, Eye, EyeClosed, Code, Image, Video,
            Cuboid, Box, Boxes, Circle, CircleDashed,
        ];

        var sw = new UnsafeSpanWriter(IconBuffer);
        for (int i = 0; i < icons.Length; i++)
        {
            sw.SetCursor(i * 4);
            sw.Append(icons[i]);
        }
    }

    private static void InitColors()
    {
        ColorBuffer[(int)SceneObjectKind.Empty] = Palette.GrayLight;
        ColorBuffer[(int)SceneObjectKind.Model] = Palette.Model;
        ColorBuffer[(int)SceneObjectKind.Particle] = Palette.CyanLight;

        _assetColorPtr = ColorBuffer.Ptr + 3;
        _assetColorPtr[(int)AssetKind.Unknown] = Palette.GrayLight;
        _assetColorPtr[(int)AssetKind.Shader] = Palette.Shader;
        _assetColorPtr[(int)AssetKind.Model] = Palette.Model;
        _assetColorPtr[(int)AssetKind.Texture] = Palette.Texture;
        _assetColorPtr[(int)AssetKind.Material] = Palette.Material;

        _logLevelPtr = _assetColorPtr + 5;
        _logLevelPtr[(int)LogLevel.None] = Color4.White;
        _logLevelPtr[(int)LogLevel.Trace] = Palette.GrayLight;
        _logLevelPtr[(int)LogLevel.Debug] = Palette.BlueLight;
        _logLevelPtr[(int)LogLevel.Info] = Palette.GreenBase;
        _logLevelPtr[(int)LogLevel.Warn] = Palette.OrangeBase;
        _logLevelPtr[(int)LogLevel.Error] = Palette.RedBase;
        _logLevelPtr[(int)LogLevel.Critical] = Palette.RedLight;
    }
}