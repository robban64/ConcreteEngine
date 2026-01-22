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

    public override AssetObjectProxy GetAssetProxy(AssetId assetId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assetId.Value, nameof(assetId));

        if (!_store.TryGet(assetId, out var asset))
            throw new ArgumentException($"Asset {assetId} does not exist");

        var fileSpecs = FetchAssetFileSpecs(assetId);

        IAssetProxyProperty? property = asset.Kind switch
        {
            AssetKind.Shader => new ShaderProxyProperty((Shader)asset),
            AssetKind.Model => MakeModelProxy((Model)asset),
            AssetKind.Texture => new TextureProxyProperty((Texture)asset),
            AssetKind.Material => MakeMaterialProxy((Material)asset),
            _ => throw new ArgumentOutOfRangeException(nameof(asset.Kind))
        };

        return new AssetObjectProxy(asset, fileSpecs) { Property = property };
    }

    private ModelProxyProperty MakeModelProxy(Model model)
    {
        var meshLen = model.Meshes.Length;
        var meshes = new ModelProxyProperty.MeshPart[meshLen];
        for (var i = 0; i < meshLen; i++)
        {
            var it = model.Meshes[i];
            meshes[i] = new ModelProxyProperty.MeshPart(it.Name, it.GfxId, it.Spec);
        }

        var clips = Array.Empty<ModelProxyProperty.Clip>();
        int boneCount = 0;
        if (model.Animation is { } anim)
        {
            boneCount = anim.BoneCount;
            var clipLen = anim.ClipDataSpan.Length;
            clips = new ModelProxyProperty.Clip[clipLen];
            for (var i = 0; i < clipLen; i++)
            {
                var it = anim.ClipDataSpan[i];
                clips[i] = new ModelProxyProperty.Clip(it.Name, it.TrackCount, it.Duration, it.TicksPerSecond);
            }
        }

        return new ModelProxyProperty(model) { Meshes = meshes, Clips = clips, BoneCount = boneCount };
    }


    private MaterialProxyProperty MakeMaterialProxy(Material material)
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
            if (source.Texture.IsValid()) textures[i] = _store.Get<Texture>(source.Texture);
            else textures[i] = null!;
        }

        material.FillParams(out var param);
        return new MaterialProxyProperty(material, in param, material.Pipeline)
        {
            TemplateMaterial = template, Shader = shader, Bindings = sources, Textures = textures,
        };
    }
}