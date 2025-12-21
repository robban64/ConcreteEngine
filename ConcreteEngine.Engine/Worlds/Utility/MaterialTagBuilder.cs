using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds.Utility;

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


    public static MaterialTag FromSpan(Span<MaterialMeta> materials)
    {
        var builder = new MaterialTagBuilder();
        foreach (var meta in materials) builder.WithSlot(meta.MaterialId, meta.HasTransparency);
        return builder.Build();
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