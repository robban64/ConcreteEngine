using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Logging;

namespace ConcreteEngine.Editor.Core.Data;

internal static class TextBuffers
{
    public static NativeArray<byte> StyleBuffer;
    public static NativeArray<byte> LogBuffer;

    private static NativeArray<byte> _scratchBuffer;

    public static ArenaAllocator PersistentArena = null!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeSpanWriter GetWriter() => new(_scratchBuffer);


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AllocateBuffers()
    {
        if (PersistentArena != null)
            throw new InvalidOperationException("Already allocated text buffers");

        _scratchBuffer = NativeArray.Allocate<byte>(512);

        StyleBuffer = NativeArray.Allocate<byte>(StyleMap.AllocSize);
        StyleMap.Allocate(StyleBuffer);

        LogBuffer = NativeArray.Allocate<byte>(ConsoleService.LogStride * ConsoleService.StoredLogCap);

        PersistentArena = new ArenaAllocator(CapacityUtils.PageSize * 2);

    }

    public static void Dispose()
    {
        StyleBuffer.Dispose();
        PersistentArena.Dispose();
        LogBuffer.Dispose();
        _scratchBuffer.Dispose();
    }
}