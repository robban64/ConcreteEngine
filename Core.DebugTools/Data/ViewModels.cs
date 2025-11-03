namespace Core.DebugTools.Data;

public sealed class AssetStoreViewModel
{
    public List<AssetObjectViewModel> AssetObjects { get; } = new(16);
}

public sealed class AssetObjectViewModel
{
    public string Name { get; set; }
    public int AssetId { get; set; }
    public bool IsCoreAsset { get; set; }
    public int Generation {get; set; }
    public string Kind { get; set; }
    
}