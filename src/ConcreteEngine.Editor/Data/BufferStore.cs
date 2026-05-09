using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Theme;

namespace ConcreteEngine.Editor.Data;

internal static class TextBuffers
{

    public static NativeArray<byte> StyleBuffer;
    public static NativeArray<byte> LogBuffer;

    private static NativeArray<byte> _scratchBuffer;

    public static ArenaAllocator PersistentArena = null!;
    public static MemoryBlockPtr WindowMemory1;
    public static MemoryBlockPtr WindowMemory2;
    public static MemoryBlockPtr WindowMemory3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeSpanWriter GetWriter() => new(_scratchBuffer);


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AllocateBuffers()
    {
        if (PersistentArena != null)
            throw new InvalidOperationException("Already allocated text buffers");

        _scratchBuffer = NativeArray.Allocate<byte>(256);

        StyleBuffer = NativeArray.Allocate<byte>(StyleMap.AllocSize);
        StyleMap.Allocate(StyleBuffer);

        LogBuffer = NativeArray.Allocate<byte>(ConsoleService.LogStride * ConsoleService.StoredLogCap);

        PersistentArena = new ArenaAllocator(1024 * 16);
        WindowMemory1 = PersistentArena.Alloc(512);
        WindowMemory2 = PersistentArena.Alloc(512);
        WindowMemory3 = PersistentArena.Alloc(512);
    }

    public static void Dispose()
    {
        StyleBuffer.Dispose();
        PersistentArena.Dispose();
        LogBuffer.Dispose();
    }
}