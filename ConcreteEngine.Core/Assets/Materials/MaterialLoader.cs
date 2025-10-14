#region

using System.Diagnostics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Internal;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

internal sealed class MaterialLoader
{
    public List<MaterialTemplate>? LoadMaterials(AssetStore store, MaterialDescriptor[] descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        
        if (descriptors.Length == 0)
        {
            Debug.Assert(false);
            return null;
        }

        var result = new List<MaterialTemplate>();
        AssetAssembleDel<MaterialTemplate, MaterialDescriptor> factory = CreateMaterial;
        foreach (var record in descriptors)
        {
            var template = store.Register(record, factory);
            result.Add(template);
        }

        foreach (var mat in result)
            mat.Initialize(store);

        return result;
    }

    private MaterialTemplate CreateMaterial(AssetId assetId, MaterialDescriptor record, IAssetStore store)
    {
        var textures = Array.Empty<AssetRef<Texture2D>>();
        AssetRef<CubeMap>? cubeMap = null;

        if (record.CubeMap != null)
        {
            cubeMap = store.GetByName<CubeMap>(record.CubeMap).RefId;
        }
        else if (record.Textures != null)
        {
            textures = new AssetRef<Texture2D>[record.Textures.Length];
            for (var i = 0; i < record.Textures.Length; i++)
            {
                textures[i] = store.GetByName<Texture2D>(record.Textures[i]).RefId;
            }
        }

        var shader = store.GetByName<Shader>(record.Shader).RefId;

        return new MaterialTemplate
        {
            RawId = assetId,
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