#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Rendering.State;

public enum BeginFrameStatus
{
    None,
    Resize
}

public readonly record struct RenderFrameInfo(long FrameIndex, float DeltaTime, float Alpha, Size2D OutputSize)
{
    public float Fps => DeltaTime > 0 ? 1.0f / DeltaTime : 0.0f;

    public GfxFrameInfo ToGfxFrameInfo() => new(FrameIndex, DeltaTime, OutputSize);
}

public readonly record struct RenderRuntimeParams(Size2D ScreenSize, Vector2 MousePos, float Time, int RndSeed);