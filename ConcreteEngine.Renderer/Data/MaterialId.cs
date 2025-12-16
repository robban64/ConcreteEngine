using System.Runtime.InteropServices;

namespace ConcreteEngine.Renderer.Data;


[StructLayout(LayoutKind.Sequential)]
public readonly record struct MaterialId: IComparable<MaterialId>
{
    public readonly ushort Id;

    public MaterialId(ushort value) => Id = value;
    public MaterialId(int value) => Id = (ushort)value;

    public static implicit operator int(MaterialId id) => id.Id;

    public int CompareTo(MaterialId other) => Id.CompareTo(other.Id);
}
