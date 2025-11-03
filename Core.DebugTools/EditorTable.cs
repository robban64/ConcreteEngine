using Core.DebugTools.Data;

namespace Core.DebugTools;

public static class EditorTable
{
    public static Action<AssetStoreViewModel>? FillAssetStoreView { get; set; }
    public static Func<AssetObjectViewModel, List<AssetObjectFileViewModel>>? FetchAssetObjectFiles { get; set; }


}