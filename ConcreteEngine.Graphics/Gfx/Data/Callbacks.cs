using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Data;

public readonly struct GfxMetaChanged<TMeta>(
    int Id,
    in TMeta meta,
    ushort generation,
    bool recreated,
    ResourceKind kind) where TMeta : unmanaged, IResourceMeta
{
    public readonly int Id = Id;
    public readonly TMeta Meta = meta;
    public readonly ushort Generation = generation;
    public readonly ResourceKind Kind = kind;
    public readonly bool Recreated = recreated;
}