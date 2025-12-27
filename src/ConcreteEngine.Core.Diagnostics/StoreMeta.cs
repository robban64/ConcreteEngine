namespace ConcreteEngine.Core.Diagnostics;

public struct GfxStoreMeta(
    in CollectionSample fk,
    in CollectionSample bk,
    in GfxMetaInfo metaInfo,
    byte kind)
{
    public CollectionSample Fk = fk;
    public CollectionSample Bk = bk;
    public GfxMetaInfo MetaInfo = metaInfo;
    public byte Kind = kind;
}

public readonly struct GfxMetaInfo(
    long value,
    int resourceId,
    ushort param2 = 0)
{
    public readonly long Value = value;
    public readonly int ResourceId = resourceId;
    public readonly ushort Param2 = param2;
}
