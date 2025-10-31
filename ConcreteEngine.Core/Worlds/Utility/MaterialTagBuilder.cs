using ConcreteEngine.Core.Worlds.Data;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Core.Worlds.Utility;

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


    public MaterialTagBuilder WithSlot(MaterialSlotInfo info)
    {
        var slot = info.Slot;
        var material = info.Material;
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

        if (info.IsTransparent)
            _transparentMask = (byte)(_transparentMask | (1 << slot));

        return this;
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
}