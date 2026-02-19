using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class AssetApiController(ApiContext context) : AssetController
{
    private readonly AssetStore _store = context.AssetStore;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override TextureId GetTextureId(AssetId id, out TextureKind kind)
    {
        var texture = _store.Get<Texture>(id);
        kind = texture.TextureKind;
        return texture.GfxId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string GetAssetName(AssetId id) => _store.Get(id).Name;

    // Todo proper search
    public override int FilterQuery(in SearchPayload<AssetId> search, SearchAssetFilter filter, SearchAssetDel del)
    {
        if (filter.Kind == AssetKind.Unknown) return 0;

        if (filter.Kind == AssetKind.Texture)
            return SearchTextures(in search, filter, del);

        if (filter.Kind == AssetKind.Model)
            return SearchModels(in search, filter, del);


        return SearchAsset(in search, filter, del);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SearchAsset(in SearchPayload<AssetId> search, SearchAssetFilter filter, SearchAssetDel del)
    {
        var count = 0;
        var assetList = _store.GetAssetList(filter.Kind);
        foreach (var it in assetList.GetAssetObjects())
        {
            var item = new AssetQueryItem(it.Name, it.PackedName, (ushort)it.Generation, it.Kind);
            if (del(in search, filter, in item))
                search.Destination[count++] = it.Id;

            if (count >= EditorConsts.AssetCapacity) return count;
        }
        return count;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SearchTextures(in SearchPayload<AssetId> search, SearchAssetFilter filter, SearchAssetDel del)
    {
        var count = 0;
        var assetList = _store.GetAssetList<Texture>();
        foreach (var it in assetList.GetAssets())
        {
            if (filter.Filter > 0 && (int)it.TextureKind != filter.Filter) continue;
            var item = new AssetQueryItem(it.Name, it.PackedName, (ushort)it.Generation, it.Kind);
            if (del(in search, filter, in item))
                search.Destination[count++] = it.Id;

            if (count >= EditorConsts.AssetCapacity) return count;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SearchModels(in SearchPayload<AssetId> search, SearchAssetFilter filter, SearchAssetDel del)
    {
        var count = 0;
        var assetList = _store.GetAssetList<Model>();
        foreach (var it in assetList.GetAssets())
        {
            if (filter.Filter > 0)
            {
                if (filter.Filter == 1 && it.AnimationId > 0) continue;
                if (filter.Filter == 2 && it.AnimationId == 0) continue;
            }
            var item = new AssetQueryItem(it.Name, it.PackedName, (ushort)it.Generation, it.Kind);
            if (del(in search, filter, in item))
                search.Destination[count++] = it.Id;

            if (count >= EditorConsts.AssetCapacity) return count;
        }
        return count;
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
            AssetKind.Shader => MakeShaderProxy((Shader)asset),
            AssetKind.Model => MakeModelProxy((Model)asset),
            AssetKind.Texture => MakeTextureProxy((Texture)asset),
            AssetKind.Material => MakeMaterialProxy((Material)asset),
            _ => throw new ArgumentOutOfRangeException(nameof(asset.Kind))
        };

        return new AssetObjectProxy(asset, fileSpecs) { Property = property };
    }


    private ShaderProxyProperty MakeShaderProxy(Shader shader)
    {
        return new ShaderProxyProperty(shader);
    }

    private TextureProxyProperty MakeTextureProxy(Texture texture)
    {
        return new TextureProxyProperty(texture);
    }

    private ModelProxyProperty MakeModelProxy(Model model)
    {
        var inspectorObject = InspectorBuilder.Build(typeof(Model), model);
        var props = new ModelProxyProperty(model, inspectorObject);
        return props;

        /*
        var meshLen = model.Meshes.Length;
        var meshes = new ModelProxyProperty.MeshPart[meshLen];
        for (var i = 0; i < meshLen; i++)
        {
            var it = model.Meshes[i];
            meshes[i] = new ModelProxyProperty.MeshPart(it.Name, it.MeshId, it.Info);
        }

        var clips = Array.Empty<ModelProxyProperty.Clip>();
        int boneCount = 0;
        if (model.Animation is { } anim)
        {
            boneCount = anim.BoneCount;
            var clipLen = anim.AnimationCount;
            clips = new ModelProxyProperty.Clip[clipLen];
            for (var i = 0; i < clipLen; i++)
            {
                var it = anim.Clips[i];
                clips[i] = new ModelProxyProperty.Clip(it.Name, it.Channels.Length, it.Duration, it.TicksPerSecond);
            }
        }

        return new ModelProxyProperty(model) { Meshes = meshes, Clips = clips, BoneCount = boneCount, };
        */
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
            TemplateMaterial = template,
            Shader = shader,
            Bindings = sources,
            Textures = textures,
            CommitDel = (prop) =>
            {
                var mat = (Material)prop.Asset;
                mat.Pipeline = prop.Pipeline;
                mat.SetParams(in prop.Params);
            },
            FetchDel = (prop) =>
            {
                var mat = (Material)prop.Asset;
                prop.Pipeline = mat.Pipeline;
                mat.FillParams(out prop.Params);
            }
        };
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