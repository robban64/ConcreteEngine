namespace ConcreteEngine.Core.Assets;

public interface IAssetFile
{
    string Name { get; init; }
    string Path { get; init; }
    AssetFileType AssetType { get; }
}