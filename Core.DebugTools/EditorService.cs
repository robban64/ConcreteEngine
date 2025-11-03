using Core.DebugTools.Data;
using Core.DebugTools.Gui;

namespace Core.DebugTools;

public sealed class EditorService
{
    private readonly AssetStoreGui _assetStoreGui;
    public AssetStoreViewModel AssetStoreViewModel { get; }

    public EditorService()
    {
        AssetStoreViewModel = new AssetStoreViewModel();
        _assetStoreGui = new AssetStoreGui(AssetStoreViewModel);
    }
    
    public void RefreshAssetStoreDetailed()
    {
        EditorTable.FillAssetStoreView?.Invoke(AssetStoreViewModel);
    }

}