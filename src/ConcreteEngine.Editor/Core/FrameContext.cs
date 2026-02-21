using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Core;

internal readonly ref struct FrameContext(in NativeArray<byte> buffer, float deltaTime)
{
    private readonly ref readonly  NativeArray<byte> _buffer = ref buffer;
    public readonly float DeltaTime = deltaTime;

    public UnsafeSpanWriter Writer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(in _buffer);
    }
}