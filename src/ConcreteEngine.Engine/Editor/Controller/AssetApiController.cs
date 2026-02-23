using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class AssetApiController(ApiContext context) : AssetController
{
    private readonly AssetStore _store = context.AssetStore;
    
    public override AssetObject GetAsset(AssetId id) => _store.Get(id);
    public override T GetAsset<T>(AssetId id) => _store.Get<T>(id);
    
    public override bool TryGetAsset(AssetId id, out AssetObject asset) => _store.TryGet(id, out asset);
    public override bool TryGetAsset<T>(AssetId id, out T asset) => _store.TryGet<T>(id, out asset);

    public override ReadOnlySpan<AssetObject> GetAssetSpan(AssetKind kind) =>
        _store.GetAssetList(kind).GetAssetObjectSpan();

    public override ReadOnlySpan<T> GetAssetSpan<T>() => _store.GetAssetList<T>().GetAssetSpan();
    
    public override AssetFileSpec[] GetAssetFileSpecs(AssetId assetId)
    {
        _store.TryGetFileIds(assetId, out var fileIds);

        if (fileIds.Length == 0 || !_store.TryGet(assetId, out _)) return [];

        var result = new AssetFileSpec[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
            _store.TryGetFileEntry(fileIds[i], out result[i]);

        return result;
    }

}

/*
    public AssetInfo Create(AssetId assetId)
    {
        var asset  = _store.Get<Texture>(assetId);
        return new AssetInfo(assetId, asset.Name, asset.Generation, asset.Kind)
        {
            Fields = [
                new ResourceProperty<Size2D>
                {
                    Name = nameof(Texture.Size),
                    Kind = FieldKind.Struct,
                    Group = FieldGroup.General,
                    Order = 0,
                    Get = () => asset.Size
                },
                new ResourceProperty<int>
                {
                    Name = nameof(Texture.MipLevels),
                    Kind = FieldKind.Number,
                    Group = FieldGroup.General,
                    Order = 0,
                    Get = () => asset.MipLevels
                },
                new ResourceProperty<float>
                {
                    Name = nameof(Texture.LodBias),
                    Kind = FieldKind.Number,
                    Group = FieldGroup.General,
                    Order = 0,
                    Get = () => asset.LodBias,
                },
                new ResourceProperty<int>
                {
                    Name = nameof(Texture.Preset),
                    Kind = FieldKind.Enum,
                    Group = FieldGroup.General,
                    Order = 0,
                    Get = () => (int)asset.Preset,
                },
                new ResourceProperty<int>
                {
                    Name = nameof(Texture.Anisotropy),
                    Kind = FieldKind.Enum,
                    Group = FieldGroup.General,
                    Order = 0,
                    Get = () => (int)asset.Anisotropy,
                },
                new ResourceProperty<int>
                {
                    Name = nameof(Texture.Usage),
                    Kind = FieldKind.Enum,
                    Group = FieldGroup.General,
                    Order = 0,
                    Get = () => (int)asset.Usage,
                },
                new ResourceProperty<int>
                {
                    Name = nameof(Texture.PixelFormat),
                    Kind = FieldKind.Enum,
                    Group = FieldGroup.General,
                    Order = 0,
                    Get = () => (int)asset.PixelFormat,
                },

            ]
        };
    }
*/