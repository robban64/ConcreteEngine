using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Data;

internal ref struct FileScanInfo(byte index, AssetKind kind, AssetStorageKind storageKind, bool isValid = true)
{
    public long SizeBytes;
    public DateTime LastWriteTime;
    public string? Source;

    public AssetKind Kind = kind;
    public AssetStorageKind StorageKind = storageKind;
    public bool IsValid = isValid;
    public byte FileIndex = index;
}