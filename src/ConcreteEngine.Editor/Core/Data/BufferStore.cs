using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Logging;

namespace ConcreteEngine.Editor.Core.Data;

internal static class TextBuffers
{
    //todo move
    public static NativeArray<byte> LogBuffer;

    private static NativeArray<byte> _scratchBuffer;

    // todo remove
    public static ArenaAllocator PersistentArena = null!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeSpanWriter GetWriter() => new(_scratchBuffer);


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AllocateBuffers()
    {
        if (PersistentArena != null)
            throw new InvalidOperationException("Already allocated text buffers");

        _scratchBuffer = NativeArray.Allocate<byte>(512);
        LogBuffer = NativeArray.Allocate<byte>(LogConsts.LogStride * LogConsts.StoredLogCap);
        PersistentArena = new ArenaAllocator(CapacityUtils.PageSize * 2);

    }

    public static void Dispose()
    {
        PersistentArena.Dispose();
        LogBuffer.Dispose();
        _scratchBuffer.Dispose();
    }
}