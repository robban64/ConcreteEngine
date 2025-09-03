#region

using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;

public enum DrawCommandId : byte
{
    Invalid,
    Tilemap,
    Sprite,
    Light,
    Diffuse
}

public enum DrawCommandTag : byte
{
    Invalid,
    Mesh2D,
    Effect2D,
    Mesh3D,
}

[Flags]
public enum DrawCommandFlags : byte
{
    None = 0,
    Visible = 1 << 0,
    DoubleSided = 1 << 1,
    CastShadows = 1 << 2,
    ReceiveShadows = 1 << 3,

    Shadows = CastShadows | ReceiveShadows,
}

public readonly struct DrawCommandMeta(
    DrawCommandId id,
    DrawCommandTag tag,
    RenderTargetId target,
    byte layer,
    byte view = 0,
    byte queue = 0,
    ushort depthKey = 0)
{
    public readonly DrawCommandId Id = id;
    public readonly DrawCommandTag Tag = tag;
    public readonly RenderTargetId Target = target;
    public readonly byte Layer = layer;
    public readonly byte View = view;
    public readonly byte Queue = queue;
    public readonly ushort DepthKey = depthKey;
}