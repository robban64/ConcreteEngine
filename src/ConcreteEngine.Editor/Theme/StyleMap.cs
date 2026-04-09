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
    public const int IconCount = 36;

    private static NativeView<byte> _iconsPtr = NativeView<byte>.MakeNull();
    private static NativeView<uint> _colorPtr = NativeView<uint>.MakeNull();
    private static RangeU16 _assetColorHandle;
    private static RangeU16 _logLevelColorHandle;

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
    public static uint GetSceneColor(SceneObjectKind kind) => _colorPtr[(byte)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetAssetColor(AssetKind kind) => _colorPtr[_assetColorHandle.Offset16 + (byte)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetLogLevelColor(LogLevel level) => _colorPtr[_logLevelColorHandle.Offset16 + (byte)level];

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

        ReadOnlySpan<char> icons =
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

        _colorPtr[(int)SceneObjectKind.Empty] = Palette32.GrayLight;
        _colorPtr[(int)SceneObjectKind.Model] = Palette32.Model;
        _colorPtr[(int)SceneObjectKind.Particle] = Palette.CyanLight.ToPackedRgba();

        _assetColorHandle = new RangeU16(EnumCache<SceneObjectKind>.Count, EnumCache<AssetKind>.Count);
        var assetColor = _colorPtr.Slice(_assetColorHandle);
        assetColor[(int)AssetKind.Unknown] = Palette32.GrayLight;
        assetColor[(int)AssetKind.Shader] = Palette32.Shader;
        assetColor[(int)AssetKind.Model] = Palette32.Model;
        assetColor[(int)AssetKind.Texture] = Palette32.Texture;
        assetColor[(int)AssetKind.Material] = Palette32.Material;

        _logLevelColorHandle = new RangeU16(_assetColorHandle.End, EnumCache<LogLevel>.Count);
        var logLevelColor = _colorPtr.Slice(_logLevelColorHandle);
        logLevelColor[(int)LogLevel.None] = Palette32.White;
        logLevelColor[(int)LogLevel.Trace] = Palette32.GrayLight;
        logLevelColor[(int)LogLevel.Debug] = Palette.BlueLight.ToPackedRgba();
        logLevelColor[(int)LogLevel.Info] = Palette.GreenBase.ToPackedRgba();
        logLevelColor[(int)LogLevel.Warn] = Palette.OrangeBase.ToPackedRgba();
        logLevelColor[(int)LogLevel.Error] = Palette.RedBase.ToPackedRgba();
        logLevelColor[(int)LogLevel.Critical] = Palette.RedLight.ToPackedRgba();
    }
}