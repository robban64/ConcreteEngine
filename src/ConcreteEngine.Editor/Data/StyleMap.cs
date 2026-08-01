using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.App.Theme;
using Palette = ConcreteEngine.Editor.App.Theme.Palette;

namespace ConcreteEngine.Editor.Data;

internal static class StyleMap
{
    private static NativeArray<uint> _buffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetLogLevelColor(LogLevel level) => _buffer[(byte)level];

    public static void Create()
    {
        var size =  LogLevelExt.Count * sizeof(uint);
        _buffer = NativeArray.Allocate<uint>(size);
        _buffer[(int)LogLevel.None] = Palette32.White;
        _buffer[(int)LogLevel.Trace] = Palette32.TextSecondary;
        _buffer[(int)LogLevel.Debug] = Palette.BlueLight.ToPackedRgba();
        _buffer[(int)LogLevel.Info] = Palette.GreenBase.ToPackedRgba();
        _buffer[(int)LogLevel.Warn] = Palette.OrangeBase.ToPackedRgba();
        _buffer[(int)LogLevel.Error] = Palette.RedBase.ToPackedRgba();
        _buffer[(int)LogLevel.Critical] = Palette.RedLight.ToPackedRgba();
    }

    public static void Dispose() => _buffer.Dispose();
}