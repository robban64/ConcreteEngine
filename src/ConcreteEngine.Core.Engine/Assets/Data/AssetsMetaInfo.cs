namespace ConcreteEngine.Core.Engine.Assets.Data;

public readonly struct AssetsMetaInfo(int count, int fileCount, AssetKind kind)
{
    public readonly int Count = count;
    public readonly int FileCount = fileCount;
    public readonly AssetKind Kind = kind;
}