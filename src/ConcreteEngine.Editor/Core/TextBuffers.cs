using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Theme;

namespace ConcreteEngine.Editor.Core;

internal static unsafe class TextBuffers
{
    public static NativeArray<byte> StyleBuffer;
    public static NativeArray<byte> LogBuffer;

    private static NativeViewPtr<byte> _writerPtr;
    public static ArenaAllocator PersistentArena = null!;

    public static UnsafeSpanWriter GetWriter() => new(_writerPtr);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AllocateBuffers()
    {
        if (PersistentArena != null)
            throw new InvalidOperationException("Already allocated text buffers");

        StyleBuffer = NativeArray.Allocate<byte>(StyleMap.GetSizeInBytes());
        StyleMap.Allocate(StyleBuffer);

        PersistentArena = new ArenaAllocator(1024 * 20);
        _writerPtr = PersistentArena.Alloc(256)->DataPtr;

        LogBuffer = NativeArray.Allocate<byte>(ConsoleService.LogStride * ConsoleService.StoredLogCap);
    }

    public static void Dispose()
    {
        StyleBuffer.Dispose();
        PersistentArena.Dispose();
        LogBuffer.Dispose();
    }
}