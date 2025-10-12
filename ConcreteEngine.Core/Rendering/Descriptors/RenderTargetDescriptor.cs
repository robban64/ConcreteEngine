#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering.Descriptors;

public sealed record RenderTargetDescriptor
{
    public required SceneTargetDesc SceneTarget { get; init; }
    public LightTargetDesc LightTarget { get; init; }
    public PostEffectTargetDesc PostEffectTarget { get; init; }
    public required ScreenTargetDesc ScreenTarget { get; init; }
}

public sealed record ShadowTargetDesc
{
    public required ShaderId ShadowShader { get; init; }
    public required Vector2D<int> ShadowMapSize { get; init; }
}

public sealed record SceneTargetDesc
{
    public required int Samples { get; init; }
    public required Color4 ClearColor { get; init; }
}

public sealed record LightTargetDesc
{
    public required ShaderId LightShaderId { get; init; }
    public Color4 ClearColor { get; init; } = Color4.Black;
    public BlendMode Blend { get; init; } = BlendMode.Additive;
    public TexturePreset TexPreset { get; init; } = TexturePreset.LinearMipmapRepeat;
    public Vector2 SizeRatio { get; init; } = Vector2.One;
}

public sealed record PostEffectTargetDesc
{
    public required ShaderId EffectShaderId { get; init; }
    public required ShaderId CompositeShaderId { get; init; }

    public Vector2 SizeRatio { get; init; } = Vector2.One;
}

public sealed record ScreenTargetDesc
{
    public required ShaderId ScreenShaderId { get; init; }
}