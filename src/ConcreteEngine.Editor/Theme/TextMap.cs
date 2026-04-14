using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Editor.Theme;

internal static unsafe class TextMap
{
    private static NativeArray<byte> _textBuffer;

    private static RangeU16 LogLevelHandle;
    private static RangeU16 LogScopeHandle;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Allocate(NativeArray<byte> buffer)
    {
        if (!_textBuffer.IsNull) throw new InvalidOperationException("Already allocated");

        int iconCount = EnumCache<LogLevel>.Count;
    }

}