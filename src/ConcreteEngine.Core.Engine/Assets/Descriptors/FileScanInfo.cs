using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

internal readonly ref struct FileScanInfo(
    byte fileIndex,
    string name,
    string relativePath,
    long sizeBytes = 0,
    DateTime lastWriteTime = default,
    string? sourcePath = null,
    AssetStorage storage = AssetStorage.FileSystem,
    bool isValid = true)
{
    public readonly long SizeBytes = sizeBytes;
    public readonly DateTime LastWriteTime = lastWriteTime;

    public readonly string Name = name;
    public readonly string RelativePath = relativePath;
    public readonly string? SourcePath = sourcePath;

    public readonly byte FileIndex = fileIndex;
    public readonly AssetStorage Storage = storage;
    public readonly bool IsValid = isValid;
}