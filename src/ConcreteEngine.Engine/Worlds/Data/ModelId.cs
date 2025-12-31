using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Engine.Worlds.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ModelId
{
    public readonly ushort Value;

    public ModelId(ushort value) => Value = value;

    public ModelId(int value) => Value = (ushort)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    public static implicit operator int(ModelId id) => id.Value;
}