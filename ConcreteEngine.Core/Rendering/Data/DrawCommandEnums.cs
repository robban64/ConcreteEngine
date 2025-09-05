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

public enum DrawCommandTag : byte
{
    Invalid,
    Mesh2D,
    Effect2D,
    Mesh3D,
    Terrain,
    Skybox
}

public enum DrawCommandQueue : byte
{
    None = 0,
    
    OpaqueTerrain = 10,
    Skybox = 20,

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

