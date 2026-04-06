using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Editor.Utils;

internal sealed class StringArena
{
    public const int MinCapacity = 512;
    
    private NativeViewPtr<byte> _memory;

    public StringArena(NativeViewPtr<byte> memory)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(memory.Length, MinCapacity);
        _memory = memory;
    }

    public void AllocUtf8(ReadOnlySpan<char> text)
    {
        
    }
}