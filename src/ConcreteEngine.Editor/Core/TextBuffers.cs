using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal static class TextBuffers
{
    private static readonly NativeArray<byte> WriteBuffer = NativeArray.Allocate<byte>(256);
    public static UnsafeSpanWriter GetWriter() => new(WriteBuffer);

    public static readonly ArenaAllocator PersistentArena = new();
}