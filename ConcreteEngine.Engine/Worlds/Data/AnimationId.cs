#region

using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Engine.Worlds.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct AnimationId
{
    public readonly ushort Value;

    public AnimationId(ushort value) => Value = value;
    public AnimationId(int value) => Value = (ushort)value;

    public static implicit operator int(AnimationId id) => id.Value;
}