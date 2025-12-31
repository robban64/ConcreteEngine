using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Data;

internal sealed class AssetStoreTypeMeta(Type type, AssetKind kind)
{
    public readonly AssetKind Kind = kind;
    public int Count { get; private set; }
    public int FileCount { get; private set; }

    public Type AssetType => type;

    public int Increment(int fileCount)
    {
        Count++;
        FileCount += fileCount;
        return Count;
    }

    public AssetStoreMeta ToSnapshot() => new(Count, FileCount, kind);
}