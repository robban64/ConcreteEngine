using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

internal  struct FileScanInfo(
    byte fileIndex,
    string name,
    string relativePath,
    long sizeBytes = 0,
    DateTime lastWriteTime = default,
    AssetStorage storage = AssetStorage.FileSystem,
    bool isValid = true)
{
    public  long SizeBytes = sizeBytes;
    public  DateTime LastWriteTime = lastWriteTime;

    public  string Name = name;
    public  string RelativePath = relativePath;

    public  byte FileIndex = fileIndex;
    public  AssetStorage Storage = storage;
    public  bool IsValid = isValid;
}