#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor;

public static class EditorApi
{
    public static Action<EditorAssetSelection, List<AssetObjectViewModel>>? FillAssetStoreView { get; set; }
    public static Action<AssetObjectViewModel, List<AssetObjectFileViewModel>>? FetchAssetObjectFiles { get; set; }
    public static Action<EntityListViewModel>? FillEntityView { get; set; }
    public static FetchCameraDataRequest? FetchCameraData { get; set; }
    public static FetchEntityDataRequest? FetchEntityData { get; set; }

    
    
}