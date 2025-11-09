using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.ViewModel;

namespace ConcreteEngine.Editor.Data;

public sealed class FillAssetsPayload
{
    public required EditorAssetSelection Selection { get; init; }
    public required List<AssetObjectViewModel>  Models { get; init; }
}

public sealed class FillAssetFilePayload
{
    public required AssetObjectViewModel SelectedModel { get; init; }
    public required List<AssetObjectFileViewModel>  Models { get; init; }
}