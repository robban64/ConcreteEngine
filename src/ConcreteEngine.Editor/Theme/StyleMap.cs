using System.Numerics;
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
    Code, Minus, Plus,
    Move3d, Scale3d, Rotate3d,
    MousePointer2, Sun, CloudFog, Sparkles,
    Undo2, Eye, EyeClosed, Image, Video,
    Cuboid, Box, Boxes, Circle, CircleDashed,
}

internal static unsafe class StyleMap
{
    private static byte* _iconsPtr;

    private static Vector4* _sceneColorPtr;
    private static Vector4* _assetColorPtr;
    private static Vector4* _logLevelPtr;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* GetIcon(Icons icon) => _iconsPtr + ((int)icon * 4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 GetSceneColor(SceneObjectKind kind) => ref _sceneColorPtr[(int)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 GetAssetColor(AssetKind kind) => ref _assetColorPtr[(int)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 GetLogLevelColor(LogLevel level) => ref _logLevelPtr[(int)level];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Allocate(ArenaAllocator allocator)
    {
        InitIcons(allocator);
        InitColors(allocator);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitIcons(ArenaAllocator allocator)
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

        var arena = allocator.Alloc(icons.Length * 4);
        _iconsPtr = arena->DataPtr;

        var sw = new UnsafeSpanWriter(arena->DataPtr);
        for (int i = 0; i < icons.Length; i++)
        {
            sw.SetCursor(i * 4);
            sw.Append(icons[i]);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitColors(ArenaAllocator allocator)
    {
        int count = EnumCache<SceneObjectKind>.Count + EnumCache<AssetKind>.Count + EnumCache<LogLevel>.Count;

        var arena = allocator.Alloc(count * Unsafe.SizeOf<Vector4>());
        _sceneColorPtr = arena->AllocSlice<Vector4>(EnumCache<SceneObjectKind>.Count);
        _sceneColorPtr[(int)SceneObjectKind.Empty] = Palette.GrayLight;
        _sceneColorPtr[(int)SceneObjectKind.Model] = Palette.Model;
        _sceneColorPtr[(int)SceneObjectKind.Particle] = Palette.CyanLight;

        _assetColorPtr = arena->AllocSlice<Vector4>(EnumCache<AssetKind>.Count);
        _assetColorPtr[(int)AssetKind.Unknown] = Palette.GrayLight;
        _assetColorPtr[(int)AssetKind.Shader] = Palette.Shader;
        _assetColorPtr[(int)AssetKind.Model] = Palette.Model;
        _assetColorPtr[(int)AssetKind.Texture] = Palette.Texture;
        _assetColorPtr[(int)AssetKind.Material] = Palette.Material;

        _logLevelPtr = arena->AllocSlice<Vector4>(EnumCache<LogLevel>.Count);
        _logLevelPtr[(int)LogLevel.None] = Color4.White;
        _logLevelPtr[(int)LogLevel.Trace] = Palette.GrayLight;
        _logLevelPtr[(int)LogLevel.Debug] = Palette.BlueLight;
        _logLevelPtr[(int)LogLevel.Info] = Palette.GreenBase;
        _logLevelPtr[(int)LogLevel.Warn] = Palette.OrangeBase;
        _logLevelPtr[(int)LogLevel.Error] = Palette.RedBase;
        _logLevelPtr[(int)LogLevel.Critical] = Palette.RedLight;
    }
}