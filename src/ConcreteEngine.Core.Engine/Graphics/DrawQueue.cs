namespace ConcreteEngine.Core.Engine.Graphics;

public enum DrawQueue : byte
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