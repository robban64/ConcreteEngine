using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor;

public static class EditorApi
{
    public static Action<EditorAssetSelection, List<AssetObjectViewModel>>? FillAssetStoreView { get; set; }
    public static Action<AssetObjectViewModel, List<AssetObjectFileViewModel>>? FetchAssetObjectFiles { get; set; }
    public static Action<EntityListViewModel> FillEntityView { get; set; }
    public static FetchCameraDataRequest FetchCameraData { get; set; }

}