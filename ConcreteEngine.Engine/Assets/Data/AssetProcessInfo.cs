namespace ConcreteEngine.Engine.Assets.Data;

internal readonly record struct AssetProcessInfo(
    AssetProcessStatus Status,
    AssetKind AssetType = AssetKind.Unknown
)
{
    public static AssetProcessInfo MakeDone() => new(AssetProcessStatus.Done);

    public static AssetProcessInfo MakeFailed() => new(AssetProcessStatus.Failed);
}