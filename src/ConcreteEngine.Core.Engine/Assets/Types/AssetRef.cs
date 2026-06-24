using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Core.Engine.Assets;

public interface IAssetListener
{
    void OnAssetChanged(AssetObject asset);
    void OnAssetRemoved(AssetObject asset);
}

public abstract class AssetRef
{
    protected AssetObject? AssetObj;
    private readonly IAssetListener _listener;

    protected AssetRef(AssetObject? assetObj, IAssetListener listener)
    {
        ArgumentNullException.ThrowIfNull(assetObj);
        ArgumentNullException.ThrowIfNull(listener);
        AssetObj = assetObj;
        _listener = listener;

        assetObj.AddRef(_listener);
    }

    public abstract AssetObject Asset { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Trigger() => _listener.OnAssetChanged(Asset);

    internal void Detach()
    {
        _listener.OnAssetRemoved(Asset);
        Asset.RemoveRef(_listener);
        AssetObj = null!;
    }
}

public sealed class AssetRef<TAsset>(TAsset assetObject, IAssetListener listener) : AssetRef(assetObject, listener)
    where TAsset : AssetObject
{
    public override TAsset Asset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (AssetObj is null) Throwers.InvalidOperation("Asset is not bound");
            return (TAsset)AssetObj;
        }
    }
}