using System.Diagnostics;
using ConcreteEngine.Core.Assets.Config;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;

namespace ConcreteEngine.Core.Assets.Materials;

internal sealed class MaterialLoader
{
    
    public List<MaterialTemplate>? LoadMaterials(AssetStore store, AssetResourceLayout layout, AssetConfigLoader configLoader)
    {
        var manifest = configLoader.LoadManifest<MaterialManifest>(layout.Material);
        ArgumentNullException.ThrowIfNull(manifest.Records);

        var records = manifest.Records;

        if (records.Length == 0)
        {
            Debug.Assert(false);
            return null;
        }

        var result = new List<MaterialTemplate>();
        AssetAssembleDel<MaterialTemplate, MaterialManifestRecord> factory = CreateMaterial;
        foreach (var record in records)
        {
            var template = store.Register(record,factory);
            result.Add(template);
        }

        foreach (var mat in result)
            mat.Initialize(store);

        return result;

       
    }
    
    private MaterialTemplate CreateMaterial(AssetId assetId, MaterialManifestRecord record, IAssetStore store)
    {
        var textures = Array.Empty<AssetId>();
        AssetId? cubeMap = null;
        
        if (record.CubeMap != null)
        {
            cubeMap = store.Get<CubeMap>(record.CubeMap).Id;
        }
        else if (record.Textures != null)
        {
            textures = new AssetId[record.Textures.Length];
            for (var i = 0; i < record.Textures.Length; i++)
            {
                textures[i] = store.Get<Texture2D>(record.Textures[i]).Id;
            }
        }

        var shader = store.Get<Shader>(record.Shader).Id;

        return new MaterialTemplate
        {
            Id = assetId,
            Name = record.Name,
            ShaderAssetId = shader,
            Color = record.Color,
            TextureAssetIds = textures,
            CubeMapAssetId = cubeMap,
            IsCoreAsset = false,
            Generation = 0
        };
    }
}