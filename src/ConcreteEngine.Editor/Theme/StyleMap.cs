using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using static ConcreteEngine.Editor.Utils.IconNames;

namespace ConcreteEngine.Editor.Theme;

public enum Icons : byte
{
    Activity, LayoutGrid, Play, Pause,
    Code, Minus, Plus,
    Move3d, Scale3d, Rotate3d,
    MousePointer2, Sun, CloudFog, Sparkles,
    Undo2, Eye, EyeClosed, Image, Video,
    Cuboid, Box, Boxes, Circle, CircleDashed,
}

internal static unsafe class StyleMap
{
    private static NativeArray<Vector4> _colorBuffer = NativeArray.Allocate<Vector4>(16);
    private static NativeArray<byte> _iconBuffer = NativeArray.Allocate<byte>(128);

    private static NativeViewPtr<Vector4> _assetColorPtr;
    private static NativeViewPtr<Vector4> _logLevelPtr;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* GetIcon(Icons icon) => _iconBuffer + ((int)icon * 4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 GetSceneColor(SceneObjectKind kind) => ref _colorBuffer[(int)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 GetAssetColor(AssetKind kind) => ref _assetColorPtr[(int)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 GetLogLevelColor(LogLevel level) => ref _logLevelPtr[(int)level];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Init()
    {
        InitIcons();
        InitColors();
    }

    public static void Dispose()
    {
        _colorBuffer.Dispose();
        _iconBuffer.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitIcons()
    {
        Span<char> icons =
        [
            Activity, LayoutGrid, Play, Pause,
            Code, Minus, Plus,
            Move3d, Scale3d, Rotate3d,
            MousePointer2, Sun, CloudFog, Sparkles,
            Undo2, Eye, EyeClosed, Image, Video,
            Cuboid, Box, Boxes, Circle, CircleDashed,
        ];

        var sw = new UnsafeSpanWriter(_iconBuffer);
        for (int i = 0; i < icons.Length; i++)
        {
            sw.SetCursor(i * 4);
            sw.Append(icons[i]);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitColors()
    {
        _colorBuffer[(int)SceneObjectKind.Empty] = Palette.GrayLight;
        _colorBuffer[(int)SceneObjectKind.Model] = Palette.Model;
        _colorBuffer[(int)SceneObjectKind.Particle] = Palette.CyanLight;

        _assetColorPtr = _colorBuffer.Slice(3, EnumCache<AssetKind>.Count);
        _assetColorPtr[(int)AssetKind.Unknown] = Palette.GrayLight;
        _assetColorPtr[(int)AssetKind.Shader] = Palette.Shader;
        _assetColorPtr[(int)AssetKind.Model] = Palette.Model;
        _assetColorPtr[(int)AssetKind.Texture] = Palette.Texture;
        _assetColorPtr[(int)AssetKind.Material] = Palette.Material;

        _logLevelPtr = _colorBuffer.Slice(_assetColorPtr.Offset + _assetColorPtr.Length, EnumCache<LogLevel>.Count);
        _logLevelPtr[(int)LogLevel.None] = Color4.White;
        _logLevelPtr[(int)LogLevel.Trace] = Palette.GrayLight;
        _logLevelPtr[(int)LogLevel.Debug] = Palette.BlueLight;
        _logLevelPtr[(int)LogLevel.Info] = Palette.GreenBase;
        _logLevelPtr[(int)LogLevel.Warn] = Palette.OrangeBase;
        _logLevelPtr[(int)LogLevel.Error] = Palette.RedBase;
        _logLevelPtr[(int)LogLevel.Critical] = Palette.RedLight;
    }
}