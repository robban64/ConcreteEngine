using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Data;

public readonly struct GfxMetaChanged<TMeta>(
    int Id,
    in TMeta meta,
    ushort generation,
    bool recreated,
    GraphicsKind kind) where TMeta : unmanaged, IResourceMeta
{
    public readonly int Id = Id;
    public readonly TMeta Meta = meta;
    public readonly ushort Generation = generation;
    public readonly GraphicsKind Kind = kind;
    public readonly bool Recreated = recreated;
}