#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Core.Rendering.Passes;

#endregion

namespace ConcreteEngine.Core.Rendering.Commands;

public readonly ref struct DrawCommandData(
    ReadOnlySpan<DrawCommand> draw,
    ReadOnlySpan<DrawCommandMeta> meta,
    ReadOnlySpan<DrawTransformPayload> transform)
{
    public readonly ReadOnlySpan<DrawCommand> Draw = draw;
    public readonly ReadOnlySpan<DrawCommandMeta> Meta = meta;
    public readonly ReadOnlySpan<DrawTransformPayload> Transform = transform;
}