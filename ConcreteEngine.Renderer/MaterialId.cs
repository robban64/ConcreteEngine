using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Renderer;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct MaterialId : IComparable<MaterialId>
{
    public readonly ushort Id;

    public MaterialId(ushort value) => Id = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MaterialId(int value) => Id = (ushort)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(MaterialId id) => id.Id;

    public int CompareTo(MaterialId other) => Id.CompareTo(other.Id);
}