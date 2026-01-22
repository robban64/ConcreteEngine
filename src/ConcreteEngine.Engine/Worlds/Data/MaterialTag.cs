namespace ConcreteEngine.Engine.Worlds.Data;
/*

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

    public bool Equals(MaterialTag other) =>
        EndIndex == other.EndIndex && TransparencyMask == other.TransparencyMask &&
        Slot0 == other.Slot0 && Slot1 == other.Slot1 && Slot2 == other.Slot2 &&
        Slot3 == other.Slot3 && Slot4 == other.Slot4 && Slot5 == other.Slot5;

    public override int GetHashCode() =>
        HashCode.Combine(Slot0, Slot1, Slot2, Slot3, Slot4, Slot5, EndIndex, TransparencyMask);
}


public struct MaterialTagBuilder()
{
    private MaterialId _s0;
    private MaterialId _s1;
    private MaterialId _s2;
    private MaterialId _s3;
    private MaterialId _s4;
    private MaterialId _s5;
    private byte _transparentMask;

    private int _currentSlot = 0;

    public static MaterialTagBuilder Start(MaterialId material, bool transparent = false)
    {
        var builder = new MaterialTagBuilder();
        return builder.WithSlot(material, transparent);
    }

    public static MaterialTag BuildOne(MaterialId material, bool transparent = false)
    {
        return Start(material, transparent).Build();
    }


    public MaterialTagBuilder WithSlot(MaterialId material, bool transparent = false)
    {
        var slot = _currentSlot++;
        switch (slot)
        {
            case 0: _s0 = material; break;
            case 1: _s1 = material; break;
            case 2: _s2 = material; break;
            case 3: _s3 = material; break;
            case 4: _s4 = material; break;
            case 5: _s5 = material; break;
            default: throw new ArgumentOutOfRangeException(nameof(slot));
        }

        if (transparent)
            _transparentMask = (byte)(_transparentMask | (1 << slot));

        return this;
    }

    public MaterialTag Build()
    {
        return new MaterialTag(
            _s0, _s1, _s2, _s3, _s4, _s5,
            _transparentMask
        );
    }
}*/