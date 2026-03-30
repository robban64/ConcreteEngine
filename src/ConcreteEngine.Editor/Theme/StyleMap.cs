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

public enum Icons : ushort
{
    Activity, LayoutGrid, Play, Pause, Code, Minus, Plus,
    Folder, FolderOpen, FolderClosed,
    Move3d, Scale3d, Rotate3d,
    MousePointer2, Sun, CloudFog, Sparkles,
    Undo2, Eye, EyeClosed, Image, Video,
    Cuboid, Box, Boxes, Circle, CircleDashed,
}

internal static unsafe class StyleMap
{
    private static NativeViewPtr<byte> _iconsPtr = NativeViewPtr<byte>.MakeNull();
    private static NativeViewPtr<Vector4> _colorPtr = NativeViewPtr<Vector4>.MakeNull();
    private static Vector4* _assetColorPtr = null;
    private static Vector4* _logLevelPtr = null;

    public static int GetSizeInBytes()
    {
        int colorCount = EnumCache<SceneObjectKind>.Count + EnumCache<AssetKind>.Count + EnumCache<LogLevel>.Count;
        int iconCount = EnumCache<Icons>.Count;
        return iconCount * 4 + colorCount * Unsafe.SizeOf<Vector4>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* GetIcon(Icons icon) => _iconsPtr + ((int)icon * 4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 GetSceneColor(SceneObjectKind kind) => ref _colorPtr[(int)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 GetAssetColor(AssetKind kind) => ref _assetColorPtr[(int)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Vector4 GetLogLevelColor(LogLevel level) => ref _logLevelPtr[(int)level];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Allocate(NativeArray<byte> buffer)
    {
        if (!_iconsPtr.IsNull) throw new InvalidOperationException("Already allocated");

        int iconCount = EnumCache<Icons>.Count;
        _iconsPtr = buffer.Slice(0, iconCount * 4);
        _colorPtr = buffer.SliceFrom(_iconsPtr.Length).Reinterpret<Vector4>();

        InitIcons();
        InitColors();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitIcons()
    {
        if (_iconsPtr.IsNull) throw new InvalidOperationException("Style pointer is null");

        Span<char> icons =
        [
            Activity, LayoutGrid, Play, Pause, Code, Minus, Plus,
            Folder, FolderOpen, FolderClosed,
            Move3d, Scale3d, Rotate3d,
            MousePointer2, Sun, CloudFog, Sparkles,
            Undo2, Eye, EyeClosed, Image, Video,
            Cuboid, Box, Boxes, Circle, CircleDashed,
        ];

        var sw = _iconsPtr.Writer();
        for (int i = 0; i < icons.Length; i++)
        {
            sw.SetCursor(i * 4);
            sw.Append(icons[i]);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitColors()
    {
        if (_colorPtr.IsNull) throw new InvalidOperationException("Style pointer is null");

        _colorPtr[(int)SceneObjectKind.Empty] = Palette.GrayLight;
        _colorPtr[(int)SceneObjectKind.Model] = Palette.Model;
        _colorPtr[(int)SceneObjectKind.Particle] = Palette.CyanLight;

        _assetColorPtr = _colorPtr + EnumCache<SceneObjectKind>.Count;
        _assetColorPtr[(int)AssetKind.Unknown] = Palette.GrayLight;
        _assetColorPtr[(int)AssetKind.Shader] = Palette.Shader;
        _assetColorPtr[(int)AssetKind.Model] = Palette.Model;
        _assetColorPtr[(int)AssetKind.Texture] = Palette.Texture;
        _assetColorPtr[(int)AssetKind.Material] = Palette.Material;

        _logLevelPtr = _assetColorPtr + EnumCache<AssetKind>.Count;
        _logLevelPtr[(int)LogLevel.None] = Color4.White;
        _logLevelPtr[(int)LogLevel.Trace] = Palette.GrayLight;
        _logLevelPtr[(int)LogLevel.Debug] = Palette.BlueLight;
        _logLevelPtr[(int)LogLevel.Info] = Palette.GreenBase;
        _logLevelPtr[(int)LogLevel.Warn] = Palette.OrangeBase;
        _logLevelPtr[(int)LogLevel.Error] = Palette.RedBase;
        _logLevelPtr[(int)LogLevel.Critical] = Palette.RedLight;
    }
}