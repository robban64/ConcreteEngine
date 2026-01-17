using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;

namespace ConcreteEngine.Editor.Components.State;

internal sealed class AssetState
{

    public AssetKind ShowKind;

    public AssetId SelectedId => Proxy?.Asset.Id ?? AssetId.Empty;
    public AssetProxy? Proxy;

    public ReadOnlySpan<IAsset> GeAssetSpan() => EngineController.AssetController.GetAssetSpan(ShowKind);

    public void ResetState()
    {
        ShowKind = default;
        Proxy = null;
    }
}