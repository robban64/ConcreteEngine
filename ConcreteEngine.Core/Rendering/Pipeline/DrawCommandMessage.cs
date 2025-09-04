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
    Mesh,
    Terrain,
}

public enum DrawCommandTag : byte
{
    Invalid,
    Mesh2D,
    Effect2D,
    Mesh3D,
    Terrain
}

public enum DrawCommandQueue : byte
{
    None = 0,
    Skybox = 1,
    
    Opaque = 20,
    OpaqueTerrain = 10,
    
    AlphaTest = 60,
    
    Transparent = 100,
    Particle = 101,
    
    Overlay = 200,
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
    DrawCommandQueue queue,
    byte layer = 0,
    byte view = 0,
    ushort depthKey = 0)
{
    public readonly DrawCommandId Id = id;
    public readonly DrawCommandTag Tag = tag;
    public readonly RenderTargetId Target = target;
    public readonly DrawCommandQueue Queue = queue;
    public readonly byte Layer = layer;
    public readonly byte View = view;
    public readonly ushort DepthKey = depthKey;

    public static DrawCommandMeta Make2D(DrawCommandId id, DrawCommandTag tag, RenderTargetId target, byte layer = 0)
        => new (id, tag, target, DrawCommandQueue.None, layer: layer);
}