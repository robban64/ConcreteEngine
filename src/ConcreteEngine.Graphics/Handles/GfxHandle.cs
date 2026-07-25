using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Handles;

public readonly record struct GfxHandle(ulong Value)
{
    public GfxHandle(uint value) : this((ulong)value) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(GfxHandle handle) => (uint)handle.Value;

    public bool IsValid() => Value != 0;
}