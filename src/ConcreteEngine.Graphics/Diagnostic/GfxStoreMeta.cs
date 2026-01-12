using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Graphics.Diagnostic;

public struct GfxStoreMeta(
    in CollectionSample fk,
    in CollectionSample bk,
    in GfxMetaInfo metaInfo,
    GraphicsKind kind)
{
    public CollectionSample Fk = fk;
    public CollectionSample Bk = bk;
    public GfxMetaInfo MetaInfo = metaInfo;
    public GraphicsKind Kind = kind;
}

public readonly struct GfxMetaInfo(long value, int resourceId, int param = 0)
{
    public readonly long Value = value;
    public readonly int ResourceId = resourceId;
    public readonly int Param = param;
}