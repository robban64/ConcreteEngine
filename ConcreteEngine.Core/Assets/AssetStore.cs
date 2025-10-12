namespace ConcreteEngine.Core.Assets;

internal sealed class AssetStore
{

    private readonly Dictionary<AssetFileId, AssetFileRecord> _records = new();
    private readonly Dictionary<AssetId, AssetObject> _assets = new();
    private readonly Dictionary<AssetId, AssetFileId> _bindings = new();

}