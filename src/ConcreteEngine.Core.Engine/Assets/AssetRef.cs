using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Core.Engine.Assets;

public interface IAssetListener
{
    void OnChanged(AssetObject asset);
    void OnRemoved(AssetObject asset);
}

public abstract class AssetRef
{
    protected AssetObject? AssetObjectRef;
    private readonly IAssetListener _listener;

    protected AssetRef(AssetObject? assetObjectRef, IAssetListener listener)
    {
        ArgumentNullException.ThrowIfNull(assetObjectRef);
        ArgumentNullException.ThrowIfNull(listener);
        AssetObjectRef = assetObjectRef;
        _listener = listener;
        
        assetObjectRef.AddRef(this);
    }

    public abstract AssetObject Asset { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Trigger() => _listener.OnChanged(Asset);

    internal void Detach()
    {
        _listener.OnRemoved(Asset);
        Asset.RemoveRef(this);
        AssetObjectRef = null!;
    }}

public sealed class AssetRef<TAsset>(TAsset assetObject, IAssetListener listener) : AssetRef(assetObject, listener)
    where TAsset : AssetObject
{
    public override TAsset Asset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {   
            if (AssetObjectRef is null) Throwers.InvalidOperation("Asset is not bound");
            return (TAsset)AssetObjectRef;
        }
    }
}
