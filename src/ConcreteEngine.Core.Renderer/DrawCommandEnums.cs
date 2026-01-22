namespace ConcreteEngine.Core.Renderer;

public enum DrawCommandId : byte
{
    Invalid,
    Model,
    Terrain,
    Skybox,
    Particle,
    Effect
}

public enum DrawCommandQueue : byte
{
    None = 0,
    Terrain = 20,
    Opaque = 30,
    Skybox = 40,
    Transparent = 50,
    Particles = 60,
    Additive = 70,
    Effect = 90,
    Overlay = 100,
    OverlayTransparent = 110
}

public enum DrawCommandResolver : byte
{
    None = 0,
    Wireframe = 1,
    Highlight = 2,
    BoundingVolume = 3,
}

[Flags]
public enum PassMask : ushort
{
    None = 0,
    DepthPre = 1 << 0,
    Main = 1 << 1,
    Effect = 1 << 2,
    /*ShadowDir = 1 << 2,
    ShadowSpot = 1 << 3,
    ShadowPoint = 1 << 4,
    Ui = 1 << 5,
    Post = 1 << 6,*/

    Default = DepthPre | Main //| ShadowDir | ShadowSpot | ShadowPoint
}