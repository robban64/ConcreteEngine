using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Data;

internal ref struct FileScanInfo(byte index)
{
    public long SizeBytes;
    public DateTime LastWriteTime;
    public string? ContentHash;
    public string? Source;

    public AssetKind Kind;
    public AssetStorageKind StorageKind;
    public bool IsValid;
    public byte FileIndex = index;
}