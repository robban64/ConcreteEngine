namespace ConcreteEngine.Core.Assets.Data;

public readonly record struct AssetTypeMetaSnapshot(int Count, int FileCount);

internal sealed class AssetTypeMeta(Type type)
{
    public Type ObjectType { get; } = type;
    public int Count { get; private set; } = 0;
    public int FileCount { get; private set; } = 0;

    public int Increment(int fileCount)
    {
        Count++;
        FileCount += fileCount;
        return Count;
    }
    
    public AssetTypeMetaSnapshot ToSnapshot() => new (Count, FileCount);
    
}

