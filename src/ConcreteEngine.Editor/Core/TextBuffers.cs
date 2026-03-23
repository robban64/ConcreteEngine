using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal static class TextBuffers
{
    private static NativeArray<byte> _writeBuffer = NativeArray.Allocate<byte>(256);
    public static UnsafeSpanWriter GetWriter() => new(_writeBuffer);

    public static readonly ArenaAllocator PersistentArena = new();
    public static readonly ArenaAllocator WidgetArena = new();

    public static void Dispose()
    {
        _writeBuffer.Dispose();
        PersistentArena.Dispose();
        WidgetArena.Dispose();
    }
}