using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Core.Data;

internal static class TextBuffers
{
    private static NativeArray<byte> _scratchBuffer;

    // todo remove
    public static ArenaAllocator PersistentArena = null!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeSpanWriter GetWriter() => new(_scratchBuffer);


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Create()
    {
        if (PersistentArena != null)
            throw new InvalidOperationException("Already allocated text buffers");

        _scratchBuffer = NativeArray.Allocate<byte>(512);
        PersistentArena = new ArenaAllocator(CapacityUtils.PageSize * 2);

    }

    public static void Dispose()
    {
        PersistentArena.Dispose();
        _scratchBuffer.Dispose();
    }
}