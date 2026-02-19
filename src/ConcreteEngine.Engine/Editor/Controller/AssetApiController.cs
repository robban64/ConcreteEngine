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
    
    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ListItemInfo GetTextureInfo(AssetId id, AssetKind kind)
    {
        switch (kind)
        {
            case AssetKind.Shader:
                var shader = _store.Get<Shader>(id);
                return new ListItemInfo(shader.GfxId, shader.Generation, shader.Samplers);
            case AssetKind.Model:
                var model = _store.Get<Model>(id);
                return new ListItemInfo(-1, model.Generation, model.Info.VertexCount);
            case AssetKind.Texture:
                var tex = _store.Get<Texture>(id);
                return new ListItemInfo(tex.GfxId, tex.Generation, tex.TextureKind);
            case AssetKind.Material:
                var mat = _store.Get<Material>(id);
                return new ListItemInfo(mat.AssetShader, mat.Generation, mat.TemplateId);

        }
        var texture = _store.Get<Texture>(id);
        return new TextureInfo(texture.GfxId, texture.Size, texture.TextureKind, texture.PixelFormat);
    }*/

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string GetAssetName(AssetId id) => _store.Get(id).Name;

    public override int FilterQuery(in SearchPayload<AssetId> search, SearchFilter filter,
        SearchAssetDel del)
    {
        var store = _store;
        var count = 0;
        for (var i = 1; i < EnumCache<AssetKind>.Count; i++)
        {
            var kind = (AssetKind)i;
            var filterKind = filter.AsAssetKind;
            if (filterKind != AssetKind.Unknown && filterKind != kind) continue;
            var assetList = store.GetAssetList(kind);
            foreach (var it in assetList.GetAssetObjects())
            {
                var item = new AssetQueryItem(it.Name, it.PackedName, (ushort)it.Generation, it.Kind);
                if (del(in search, filter, in item))
                    search.Destination[count++] = it.Id;

                if (count >= EditorConsts.AssetCapacity) return count;
            }
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