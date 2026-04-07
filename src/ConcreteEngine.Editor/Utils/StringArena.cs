using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Editor.Utils;

internal sealed class StringArena
{
    public const int MinCapacity = 512;

    private NativeView<byte> _memory;

    public StringArena(NativeView<byte> memory)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(memory.Length, MinCapacity);
        _memory = memory;
    }

    public void AllocUtf8(ReadOnlySpan<char> text)
    {
    }
}