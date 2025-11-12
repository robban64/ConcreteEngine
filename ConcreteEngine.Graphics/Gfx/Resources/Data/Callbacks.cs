#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

public readonly struct GfxMetaChanged(ushort generation, bool recreated, ResourceKind kind)
{
    public readonly ushort Generation = generation;
    public readonly ResourceKind Kind = kind;
    public readonly bool Recreated = recreated;
}