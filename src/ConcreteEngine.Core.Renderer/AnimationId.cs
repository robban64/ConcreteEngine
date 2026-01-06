using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Renderer;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct AnimationId
{
    public readonly ushort Value;

    public AnimationId(ushort value) => Value = value;
    public AnimationId(int value) => Value = (ushort)value;

    public int Index() => Value - 1;

    public static implicit operator int(AnimationId id) => id.Value;
}