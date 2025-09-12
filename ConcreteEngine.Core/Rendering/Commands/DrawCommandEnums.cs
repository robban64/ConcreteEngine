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
    Skybox,
}


public enum DrawCommandQueue : byte
{
    None = 0,
    Opaque = 10,
    Terrain = 20,
    Skybox = 30,
    AlphaTest = 40,
    Decals = 50,
    Transparent = 60,
    Additive = 70,
    Overlay = 100
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

