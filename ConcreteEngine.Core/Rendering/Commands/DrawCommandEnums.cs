#region

#endregion

namespace ConcreteEngine.Core.Rendering.Commands;

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
public enum PassMask : uint
{
    None = 0,
    DepthPre = 1 << 0,
    Main = 1 << 1,
    ShadowDir = 1 << 2,
    ShadowSpot = 1 << 3,
    ShadowPoint = 1 << 4,
    Ui = 1 << 5,
    Post = 1 << 6,
    
    Default = DepthPre | Main | ShadowDir | ShadowSpot | ShadowPoint

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