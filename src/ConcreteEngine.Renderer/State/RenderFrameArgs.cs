using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Renderer.State;

public enum BeginFrameStatus
{
    None,
    Resize
}

public struct RenderFrameArgs
{
    public Size2D OutputSize;
    public Vector2 MousePos;
    public float DeltaTime;
    public float Time;
    public float Alpha;
    public float Rng;
}
/*
public  struct RenderRuntimeParams(Size2D screenSize, Vector2 mousePos, float time, float rng)
{
    public  Size2D ScreenSize = screenSize;
}*/