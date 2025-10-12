using System.Diagnostics;
using ConcreteEngine.Core.Assets.Config;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Textures;

namespace ConcreteEngine.Core.Assets.Materials;

internal sealed class MaterialLoader
{
    
    public List<MaterialTemplate>? LoadMaterials(AssetStore store, AssetResourceLayout layout, AssetConfigLoader configLoader)
    {
        var manifest = configLoader.LoadManifest<MaterialManifest>(layout.Material);
        ArgumentNullException.ThrowIfNull(manifest.Records);

        var entries = manifest.Records;

        if (entries.Length == 0)
        {
            Debug.Assert(false);
            return null;
        }

        var result = new List<MaterialTemplate>();

        foreach (var entry in entries)
        {
            var mat = CreateMaterial(entry);
            store.Register((id) => CreateMaterial(entry));

            result.Add(mat);
        }

        foreach (var mat in result)
            mat.Initialize();

        return result;

       
    }
    
    private MaterialTemplate CreateMaterial(MaterialManifestRecord record)
    {
        Texture2D[] textures = [];
        CubeMap? cubeMap = null;
        if (record.CubeMap != null)
        {
            cubeMap = Get<CubeMap>(record.CubeMap);
        }
        else if (record.Textures != null)
        {
            textures = new Texture2D[record.Textures.Length];
            for (var i = 0; i < record.Textures.Length; i++)
            {
                textures[i] = Get<Texture2D>(record.Textures[i]);
            }
        }

        var shader = Get<Shader>(record.Shader);

        return new MaterialTemplate
        {
            Name = record.Name,
            Shader = shader,
            Color = record.Color,
            Textures = textures,
            CubeMap = cubeMap
        };
    }
}