namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

internal ref struct FileScanInfo(byte index, AssetKind kind, AssetStorage storage, bool isValid = true)
{
    public long SizeBytes;
    public DateTime LastWriteTime;
    public string? Source;

    public AssetKind Kind = kind;
    public AssetStorage Storage = storage;
    public bool IsValid = isValid;
    public byte FileIndex = index;
}