using ConcreteEngine.Core.Specs.Graphics;

namespace ConcreteEngine.Core.Diagnostics.Metrics;

public struct GfxStoreMeta(
    in CollectionSample fk,
    in CollectionSample bk,
    in GfxMetaInfo metaInfo,
    GraphicsHandleKind kind)
{
    public CollectionSample Fk = fk;
    public CollectionSample Bk = bk;
    public GfxMetaInfo MetaInfo = metaInfo;
    public GraphicsHandleKind Kind = kind;
}

public readonly struct GfxMetaInfo(long value, int resourceId, int param = 0)
{
    public readonly long Value = value;
    public readonly int ResourceId = resourceId;
    public readonly int Param = param;
}