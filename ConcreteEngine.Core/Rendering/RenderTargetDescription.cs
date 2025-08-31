using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderTargetDescription
{
    public required SceneTargetDesc SceneTarget { get; init; }
    public required LightTargetDesc LightTarget { get; init; }
    public required ScreenTargetDesc ScreenTarget { get; init; }
}

public sealed class SceneTargetDesc
{
    public required uint Samples { get; init; }
    public required Color4 ClearColor { get; init; }
}

public sealed class LightTargetDesc
{
    public required ShaderId LightShader { get; init; }
    public required Color4 ClearColor { get; init; }
    public required BlendMode Blend { get; init; }
    public required TexturePreset TexPreset { get; init; }
    public Vector2 SizeRatio { get; init; } = Vector2.One;
}

public sealed class ScreenTargetDesc
{
    public required ShaderId CompositeShaderId { get; init; }
}