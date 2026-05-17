using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Renderer.Core;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct MaterialId : IComparable<MaterialId>
{
    public readonly ushort Id;

    public MaterialId(int value) => Id = (ushort)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    public int CompareTo(MaterialId other) => Id.CompareTo(other.Id);

    public static MaterialId Empty = new(0);
}