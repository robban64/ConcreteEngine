using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Renderer;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ModelId
{
    public readonly ushort Value;

    public ModelId(ushort value) => Value = value;

    public ModelId(int value) => Value = (ushort)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(ModelId id) => id.Value;
    
    public static ModelId Empty = default;

}