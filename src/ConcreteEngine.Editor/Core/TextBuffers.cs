using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Theme;

namespace ConcreteEngine.Editor.Core;

internal static class TextBuffers
{
    public static ArenaAllocator PersistentArena = null!;

    public static NativeArray<byte> StyleBuffer;
    public static NativeArray<byte> LogBuffer;

    public static MemoryBlockPtr WindowMemory1;
    public static MemoryBlockPtr WindowMemory2;
    public static MemoryBlockPtr WindowMemory3;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AllocateBuffers()
    {
        if (PersistentArena != null)
            throw new InvalidOperationException("Already allocated text buffers");

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