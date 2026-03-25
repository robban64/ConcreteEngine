using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal static unsafe class TextBuffers
{
    public static ArenaAllocator PersistentArena = null!;
    public static ArenaAllocator LogArena = null!;

    private static NativeViewPtr<byte> _writerData;
    public static UnsafeSpanWriter GetWriter() => new(_writerData);


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AllocateBuffers()
    {
        if (PersistentArena != null)
            throw new InvalidOperationException("Already allocated text buffers");

        PersistentArena = new ArenaAllocator(1024 * 10);
        _writerData = PersistentArena.Alloc(256)->DataPtr;

        LogArena = new ArenaAllocator(ConsoleService.LogStride * ConsoleService.StoredLogCap +
                                      ConsoleService.StoredLogCap * ArenaBlock.BlockSize);
    }

    public static void Dispose()
    {
        PersistentArena.Dispose();
        LogArena.Dispose();
    }
}