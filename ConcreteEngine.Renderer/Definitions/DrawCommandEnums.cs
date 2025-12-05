#region

#endregion

namespace ConcreteEngine.Renderer.Definitions;

public enum DrawCommandId : byte
{
    Invalid,
    Model,
    Terrain,
    Skybox,
    Particle
}

public enum DrawCommandQueue : byte
{
    None = 0,
    Terrain = 20,
    Opaque = 30,
    Skybox = 40,
    Transparent = 50,
    Particles = 60,
    Additive = 80,
    Overlay = 100,
    OverlayTransparent = 110
}

public enum DrawCommandResolver : byte
{
    None = 0,
    Wireframe = 1,
    Highlight = 2,
    HighlightAnimated = 3,
    BoundingVolume = 4,
}

[Flags]
public enum DrawCommandFlags : byte
{
    None = 0,
    Visible = 1 << 0,
    CastShadows = 1 << 2,
    ReceiveShadows = 1 << 3,
    Static = 1 << 4,

    Shadows = CastShadows | ReceiveShadows
}