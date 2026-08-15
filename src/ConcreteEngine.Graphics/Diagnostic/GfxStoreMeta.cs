using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Graphics.Diagnostic;

public struct GfxStoreMeta(
    in StoreSample store,
    in GfxMetaInfo metaInfo,
    GraphicsKind kind)
{
    public StoreSample Store = store;
    public GfxMetaInfo MetaInfo = metaInfo;
    public GraphicsKind Kind = kind;
}

public readonly struct GfxMetaInfo(long value, int resourceId, int param = 0)
{
    public readonly long Value = value;
    public readonly int ResourceId = resourceId;
    public readonly int Param = param;
}