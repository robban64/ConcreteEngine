using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Editor.Components.State;

internal sealed class AssetState
{
    public AssetKind SelectAssetKind;

    public void ResetState()
    {
        SelectAssetKind = default;
    }
}