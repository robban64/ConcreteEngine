namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

internal readonly struct FileScanInfo(
    string name,
    string relativePath,
    long sizeBytes = 0,
    DateTime lastWriteTime = default,
    AssetStorage storage = AssetStorage.FileSystem,
    bool isValid = true)
{
    public readonly string Name = name;
    public readonly string RelativePath = relativePath;

    public readonly long SizeBytes = sizeBytes;
    public readonly DateTime LastWriteTime = lastWriteTime;

    public readonly AssetStorage Storage = storage;
    public readonly bool IsValid = isValid;
}