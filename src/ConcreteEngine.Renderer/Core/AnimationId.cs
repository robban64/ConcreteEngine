using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Renderer.Core;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct AnimationId
{
    public readonly ushort Value;

    public AnimationId(ushort value) => Value = value;
    public AnimationId(int value) => Value = (ushort)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    public static AnimationId Empty = new(0);
}