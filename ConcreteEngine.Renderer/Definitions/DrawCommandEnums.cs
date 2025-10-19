#region

#endregion

namespace ConcreteEngine.Renderer.Definitions;

public enum DrawCommandId : byte
{
    Invalid,
    Tilemap,
    Sprite,
    Light,
    Mesh,
    Terrain,
    Skybox
}

public enum DrawCommandQueue : byte
{
    None = 0,
    Skybox = 10,
    Terrain = 20,
    Opaque = 30,
    Transparent = 40,
    Additive = 60,
    Overlay = 100
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