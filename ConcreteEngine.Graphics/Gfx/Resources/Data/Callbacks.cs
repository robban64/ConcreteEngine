#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

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