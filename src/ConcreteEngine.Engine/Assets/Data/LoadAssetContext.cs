using ConcreteEngine.Engine.Assets.Descriptors;

namespace ConcreteEngine.Engine.Assets.Data;

internal ref struct LoadAssetContext(AssetId id, Guid gid, bool isCore, Func<FileSpecArgs> getFileArgs)
{
    public ReadOnlySpan<IAssetEmbeddedDescriptor> EmbeddedSpan;
    public AssetFileSpec[] FileSpecs;

    public readonly Func<FileSpecArgs> GetFileArgs = getFileArgs;
    public readonly Guid GId = gid;
    public readonly AssetId Id = id;
    public readonly bool IsCore = isCore;
}

internal readonly struct AssetIdArgs(AssetId id, Guid gId)
{
    public readonly Guid GId = gId;
    public readonly AssetId Id = id;
}

internal readonly struct FileSpecArgs(AssetFileId id, Guid gId)
{
    public readonly Guid GId = gId;
    public readonly AssetFileId Id = id;
}