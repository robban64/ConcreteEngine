#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.World.Utility;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.World.Data;

public readonly record struct MaterialTagKey(int Value);

public readonly record struct MaterialSlotInfo(MaterialId Material, ushort Slot, bool IsTransparent = false);

[StructLayout(LayoutKind.Sequential)]
public readonly record struct MaterialTag
{
    public readonly MaterialId Slot0;
    public MaterialId Slot1 { get; init; }
    public MaterialId Slot2 { get; init; }
    public MaterialId Slot3 { get; init; }
    public MaterialId Slot4 { get; init; }
    public MaterialId Slot5 { get; init; }
    public byte EndIndex { get; init; }
    public byte TransparencyMask  { get; init; } 

    
    public MaterialTag(MaterialId slot0, MaterialId slot1 = default, MaterialId slot2 = default,
        MaterialId slot3 = default, MaterialId slot4 = default,
        MaterialId slot5 = default, byte transparencyMask = 0)
    {
        Slot0 = slot0;
        Slot1 = slot1;
        Slot2 = slot2;
        Slot3 = slot3;
        Slot4 = slot4;
        Slot5 = slot5;
        TransparencyMask = transparencyMask;

        byte idx = 5;
        if (Slot0 == 0) idx = 0;
        else if (Slot1 == 0) idx = 1;
        else if (Slot2 == 0) idx = 2;
        else if (Slot3 == 0) idx = 3;
        else if (Slot4 == 0) idx = 4;
        else if (Slot5 == 0) idx = 5;

        EndIndex = idx;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsTransparent(int slot) => (TransparencyMask & (1 << slot)) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<MaterialId> AsReadOnlySpan()
        => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in Slot0), EndIndex);

}