using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Utils;
using static ConcreteEngine.Editor.Utils.IconNames;
using Palette = ConcreteEngine.Editor.App.Theme.Palette;

namespace ConcreteEngine.Editor.Core.Data;

public enum Icons : ushort
{
    ChevronLeft, ChevronRight, ChevronUp, ChevronDown, Cog,
    Activity, LayoutGrid, Database, Play, Pause, Code, Minus, Plus,
    Folder, FolderOpen, FolderClosed,
    File, FileImage, FileCode, FileBraces, FileAxis3d, FileBox, FileHeadphone, FileCog, FileChartLine,
    Move3d, Scale3d, Rotate3d,
    MousePointer2, Sun, CloudFog, Sparkles,
    Undo2, Eye, EyeClosed, Image, Video,
    Cuboid, Box, Boxes, Circle, CircleDashed,
}

internal static unsafe class StyleMap
{
    // TODO make this more proper
    public const int IconCount = 42;

    private static NativeArray<byte> _buffer;
    private static NativeView<byte> _iconsPtr = NativeView<byte>.MakeNull();
    private static NativeView<uint> _colorPtr = NativeView<uint>.MakeNull();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* GetIcon(Icons icon) => _iconsPtr + ((int)icon * 4);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetLogLevelColor(LogLevel level) => _colorPtr[(byte)level];

    public static void Create()
    {
        if (!_iconsPtr.IsNull) throw new InvalidOperationException("Already allocated");

        var size = IconCount * 4 + LogLevelExt.Count * sizeof(uint);
        var capacity =  IntMath.AlignUp(size, 64);

        _buffer = NativeArray.Allocate<byte>(capacity);
        _iconsPtr = _buffer.Slice(0, IconCount * 4);
        _colorPtr = _buffer.Slice(_iconsPtr.Length).Reinterpret<uint>();

        InitIcons();
        InitColors();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitIcons()
    {
        if (_iconsPtr.IsNull) throw new InvalidOperationException("Style pointer is null");

        ReadOnlySpan<uint> icons = stackalloc uint[]
        {
            ChevronLeft, ChevronRight, ChevronUp, ChevronDown, Cog,
            Activity, LayoutGrid, Database, Play, Pause, Code, Minus, Plus, Folder, FolderOpen, FolderClosed,
            IconNames.File, FileImage, FileCode, FileBraces, FileAxis3d, FileBox, FileHeadphone, FileCog,
            FileChartLine, Move3d, Scale3d, Rotate3d, MousePointer2, Sun, CloudFog, Sparkles, Undo2, Eye, EyeClosed,
            Image, Video, Cuboid, Box, Boxes, Circle, CircleDashed,
        };

        var sw = _iconsPtr.Writer();
        for (int i = 0; i < icons.Length; i++)
        {
            sw.SetCursor(i * 4);
            sw.AppendIcon(icons[i]).End();
        }
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitColors()
    {
        if (_colorPtr.IsNull) throw new InvalidOperationException("Style pointer is null");

        _colorPtr[(int)LogLevel.None] = Palette32.White;
        _colorPtr[(int)LogLevel.Trace] = Palette32.TextSecondary;
        _colorPtr[(int)LogLevel.Debug] = Palette.BlueLight.ToPackedRgba();
        _colorPtr[(int)LogLevel.Info] = Palette.GreenBase.ToPackedRgba();
        _colorPtr[(int)LogLevel.Warn] = Palette.OrangeBase.ToPackedRgba();
        _colorPtr[(int)LogLevel.Error] = Palette.RedBase.ToPackedRgba();
        _colorPtr[(int)LogLevel.Critical] = Palette.RedLight.ToPackedRgba();
    }

    public static void Dispose() => _buffer.Dispose();
}