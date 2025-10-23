#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

public readonly struct GfxMetaChanged<TMeta>(
    in TMeta newMeta,
    in TMeta oldMeta,
    ushort generation,
    bool recreated,
    ResourceKind kind)
    where TMeta : unmanaged, IResourceMeta
{
    public readonly TMeta OldMeta = oldMeta;
    public readonly TMeta NewMeta = newMeta;
    public readonly ushort Generation = generation;
    public readonly ResourceKind Kind = kind;
    public readonly bool Recreated = recreated;
}