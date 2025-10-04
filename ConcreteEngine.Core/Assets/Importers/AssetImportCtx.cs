namespace ConcreteEngine.Core.Assets.Importers;

internal sealed class AssetImportCtx
{
    public string Path { get; }

    internal AssetImportCtx(string path)
    {
        Path = path;
    }
}