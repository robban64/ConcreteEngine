using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
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
    File, FileImage, FileCode, FileBraces, FileAxis3d, FileBox, FileHeadphone, FileCog, FileChartLine,
    Move3d, Scale3d, Rotate3d,
    MousePointer2, Sun, CloudFog, Sparkles,
    Undo2, Eye, EyeClosed, Image, Video,
    Cuboid, Box, Boxes, Circle, CircleDashed,
}

internal static unsafe class StyleMap
{
    public static readonly int IconCount = Enum.GetValues<Icons>().Length;
    private static NativeViewPtr<byte> _iconsPtr = NativeViewPtr<byte>.MakeNull();
    private static NativeViewPtr<uint> _colorPtr = NativeViewPtr<uint>.MakeNull();
    private static uint* _assetColorPtr = null;
    private static uint* _logLevelPtr = null;

    public static int GetSizeInBytes()
    {
        int colorCount = EnumCache<SceneObjectKind>.Count + EnumCache<AssetKind>.Count + EnumCache<LogLevel>.Count;
        return IconCount * 4 + colorCount * sizeof(uint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* GetIcon(Icons icon) => _iconsPtr + ((int)icon * 4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetIntIcon(Icons icon) => ((uint*)_iconsPtr.Ptr)[(int)icon];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetSceneColor(SceneObjectKind kind) => _colorPtr[(int)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetAssetColor(AssetKind kind) => _assetColorPtr[(int)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetLogLevelColor(LogLevel level) => _logLevelPtr[(int)level];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Allocate(NativeArray<byte> buffer)
    {
        if (!_iconsPtr.IsNull) throw new InvalidOperationException("Already allocated");

        int iconCount = EnumCache<Icons>.Count;
        _iconsPtr = buffer.Slice(0, iconCount * 4);
        _colorPtr = buffer.Slice(_iconsPtr.Length).Reinterpret<uint>();

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
            IconNames.File, FileImage, FileCode, FileBraces, FileAxis3d, FileBox, FileHeadphone, FileCog, FileChartLine,
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

        _colorPtr[(int)SceneObjectKind.Empty] = Palette.GrayLight.ToPackedRgba();
        _colorPtr[(int)SceneObjectKind.Model] = Palette.Model.ToPackedRgba();
        _colorPtr[(int)SceneObjectKind.Particle] = Palette.CyanLight.ToPackedRgba();

        _assetColorPtr = _colorPtr + EnumCache<SceneObjectKind>.Count;
        _assetColorPtr[(int)AssetKind.Unknown] = Palette.GrayLight.ToPackedRgba();
        _assetColorPtr[(int)AssetKind.Shader] = Palette.Shader.ToPackedRgba();
        _assetColorPtr[(int)AssetKind.Model] = Palette.Model.ToPackedRgba();
        _assetColorPtr[(int)AssetKind.Texture] = Palette.Texture.ToPackedRgba();
        _assetColorPtr[(int)AssetKind.Material] = Palette.Material.ToPackedRgba();

        _logLevelPtr = _assetColorPtr + EnumCache<AssetKind>.Count;
        _logLevelPtr[(int)LogLevel.None] = Color4.White.ToPackedRgba();
        _logLevelPtr[(int)LogLevel.Trace] = Palette.GrayLight.ToPackedRgba();
        _logLevelPtr[(int)LogLevel.Debug] = Palette.BlueLight.ToPackedRgba();
        _logLevelPtr[(int)LogLevel.Info] = Palette.GreenBase.ToPackedRgba();
        _logLevelPtr[(int)LogLevel.Warn] = Palette.OrangeBase.ToPackedRgba();
        _logLevelPtr[(int)LogLevel.Error] = Palette.RedBase.ToPackedRgba();
        _logLevelPtr[(int)LogLevel.Critical] = Palette.RedLight.ToPackedRgba();
    }
}