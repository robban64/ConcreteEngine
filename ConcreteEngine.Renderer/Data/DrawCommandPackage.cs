namespace ConcreteEngine.Renderer.Data;

public readonly ref struct DrawCommandPackage(
    ReadOnlySpan<DrawCommand> draw,
    ReadOnlySpan<DrawCommandMeta> meta,
    ReadOnlySpan<DrawTransformPayload> transform)
{
    public readonly ReadOnlySpan<DrawCommand> Draw = draw;
    public readonly ReadOnlySpan<DrawCommandMeta> Meta = meta;
    public readonly ReadOnlySpan<DrawTransformPayload> Transform = transform;
}