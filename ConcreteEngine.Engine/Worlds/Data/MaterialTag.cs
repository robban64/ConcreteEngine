using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct MaterialTagKey
{
    public readonly ushort Value;

    public MaterialTagKey(ushort value) => Value = value;
    public MaterialTagKey(int value) => Value = (ushort)value;

    public static implicit operator int(MaterialTagKey id) => id.Value;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct MaterialSlotInfo(MaterialId material, ushort slot, bool isTransparent = false)
{
    public readonly MaterialId Material = material;
    public readonly ushort Slot = slot;
    public readonly bool IsTransparent = isTransparent;
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct MaterialTag
{
    public readonly MaterialId Slot0;
    public readonly MaterialId Slot1;
    public readonly MaterialId Slot2;
    public readonly MaterialId Slot3;
    public readonly MaterialId Slot4;
    public readonly MaterialId Slot5;
    public readonly byte EndIndex;
    public readonly byte TransparencyMask;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ResolveSlot(int slot, out MaterialId materialId)
    {
        materialId = Unsafe.Add(ref Unsafe.AsRef(in Slot0), slot);
        return (TransparencyMask & (1 << slot)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsTransparent(int slot) => (TransparencyMask & (1 << slot)) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<MaterialId> AsReadOnlySpan() =>
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in Slot0), EndIndex);


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
}