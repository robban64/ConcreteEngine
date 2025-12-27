using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Engine.Metadata;

public enum EngineGraphicsLevel : byte
{
    Low,
    Medium,
    High
}

public struct GraphicsSettings
{
    public Size2D WindowSize;
    public int FrameRate;
    public int UpdateRate;
    public bool Vsync;
    public EngineGraphicsLevel ShadowQuality;
    public EngineGraphicsLevel TextureQuality;
}