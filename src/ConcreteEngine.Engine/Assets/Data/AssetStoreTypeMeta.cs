using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Data;

internal sealed class AssetStoreTypeMeta(Type type)
{
    public Type AssetType => type;
    public int Count { get; private set; } = 0;
    public int FileCount { get; private set; } = 0;

    public int Increment(int fileCount)
    {
        Count++;
        FileCount += fileCount;
        return Count;
    }

    public AssetTypeMeta ToSnapshot() => new(Count, FileCount);
}