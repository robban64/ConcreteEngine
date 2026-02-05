using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Renderer;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct MaterialId : IComparable<MaterialId>
{
    public readonly ushort Id;

    public MaterialId(int value) => Id = (ushort)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Index() => (ushort)(Id - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(MaterialId id) => id.Id;

    public int CompareTo(MaterialId other) => Id.CompareTo(other.Id);

    public static MaterialId Empty = default;
}