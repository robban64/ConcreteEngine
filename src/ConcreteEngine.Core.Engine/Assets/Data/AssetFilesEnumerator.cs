namespace ConcreteEngine.Core.Engine.Assets.Data;

public ref struct AssetFilesEnumerator(AssetId assetId, AssetProvider provider)
{
    private int _i = -1;
    private readonly ReadOnlySpan<AssetFileId> _fileIds = provider.GetAssetFileBindings(assetId);

    public bool MoveNext() => ++_i < _fileIds.Length;
    public readonly AssetFileSpec Current => provider.GetFileSpec(_fileIds[_i]);
    
    public AssetFilesEnumerator GetEnumerator()
    {
        _i = -1;
        return this;
    }
}
