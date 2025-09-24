using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Assets;

public enum AssetProcessStatus
{
    Done,
    Repeat,
    Failed
}

internal readonly record struct AssetProcessInfo(
    AssetProcessStatus Status,
    AssetKind AssetType
)
{
    public static AssetProcessInfo MakeDone<TRecord>() where TRecord : IAssetManifestRecord =>
        new(AssetProcessStatus.Done, TRecord.Kind);

    public static AssetProcessInfo MakeRepeat<TRecord>() where TRecord : IAssetManifestRecord =>
        new(AssetProcessStatus.Repeat, TRecord.Kind);

    public static AssetProcessInfo MakeFailed<TRecord>() where TRecord : IAssetManifestRecord =>
        new(AssetProcessStatus.Failed, TRecord.Kind);
}

internal interface IAssetLoadEntry
{
    public AssetProcessInfo ProcessInfo { get; }
}

internal sealed class AssetLoadEntry<TRecord, TPayload> : IAssetLoadEntry
    where TRecord : class, IAssetManifestRecord
{
    public AssetProcessInfo ProcessInfo { get; set; }
    public TRecord Record { get; set; }
    public TPayload Payload { get; set; }
}