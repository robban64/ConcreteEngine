using Core.DebugTools.Data;
using Core.DebugTools.Definitions;

namespace Core.DebugTools;

public static class EditorTable
{
    public static Action<EditorAssetSelection, List<AssetObjectViewModel>>? FillAssetStoreView { get; set; }
    public static Action<AssetObjectViewModel, List<AssetObjectFileViewModel>>? FetchAssetObjectFiles { get; set; }


}