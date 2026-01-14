using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Engine.Assets;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class AssetApiController(ApiContext context) : AssetController
{
    private readonly AssetStore _store = context.AssetStore;

    public override ReadOnlySpan<IAsset> GetAssetSpan(AssetKind kind)
    {
        if (kind == AssetKind.Unknown) return ReadOnlySpan<AssetObject>.Empty;
        return _store.GetAssetList(kind).GetAssetObjects();
    }

    public override AssetFileSpec[] FetchAssetFileSpecs(AssetId assetId)
    {
        _store.TryGetFileIds(assetId, out var fileIds);

        if (fileIds.Length == 0 || !_store.TryGet(assetId, out _)) return [];

        var result = new AssetFileSpec[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
            _store.TryGetFileEntry(fileIds[i], out result[i]);

        return result;
    }

    public override AssetProxy GetAssetProxy(AssetId assetId, AssetKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assetId.Value, nameof(assetId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)kind, nameof(kind));

        if (!_store.TryGet(assetId, out var assetObject))
            throw new ArgumentException($"Asset {assetId} does not exist");

        if (assetObject.Kind != kind)
            throw new ArgumentException($"Asset {assetId} does not belong to asset {kind}");

        var fileSpecs = FetchAssetFileSpecs(assetId);

        IAssetProxyProperty? property = kind switch
        {
            AssetKind.Shader => new ShaderProxyProperty(),
            AssetKind.Model => new ModelProxyProperty(),
            AssetKind.Texture => new TextureProxyProperty(),
            AssetKind.Material => MakeMaterialProperty((Material)assetObject),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        return new AssetProxy(assetObject, fileSpecs) { Property = property };
    }

    private MaterialProxyProperty MakeMaterialProperty(Material material)
    {
        Material? template = null;
        if (material.TemplateId.IsValid())
            template = _store.Get<Material>(material.TemplateId);

        var shader = _store.Get<Shader>(material.AssetShader);
        var sources = material.GetTextureSources().ToArray();
        var len = sources.Length;
        var textures = new ITexture[len];
        for (var i = 0; i < len; i++)
        {
            var source = sources[i];
            if (source.IsFallback) textures[i] = null!;
            else textures[i] = _store.Get<Texture>(source.Texture);
        }

        return new MaterialProxyProperty
        {
            TemplateMaterial = template, Shader = shader, Bindings = sources, Textures = textures
        };
    }
}