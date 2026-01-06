using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Assets;

/*
 using System.Text.Json;
   using System.Text.Json.Serialization;


internal static class AssetMigrator
{
    private static readonly JsonSerializerOptions _options = JsonUtility.DefaultJsonOptions;

    public static void RunMigration(IReadOnlyList<IAssetDescriptor> descriptors)
    {
        int count = 0;
        foreach (var desc in descriptors)
        {
            try
            {
                var (record, targetPath) = ConvertToRecord(desc);

                var json = JsonSerializer.Serialize(record,  _options);
                var finalPath = targetPath + ".asset";
                File.WriteAllText(finalPath, json);

                Console.WriteLine($"[Migrated] {desc.Name} -> {Path.GetFileName(finalPath)}");
                count++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to migrate {desc.Name}: {ex.Message}");
            }
        }

        Console.WriteLine($"Migration complete. Processed {count} assets.");
    }

    private static (AssetRecord Record, string Path) ConvertToRecord(IAssetDescriptor desc)
    {
        var newGid = Guid.NewGuid();

        switch (desc)
        {
            case TextureDescriptor tex:
                {
                    if (string.IsNullOrEmpty(tex.Filename) && tex.MultiFilenames == null)
                        throw new InvalidOperationException("Texture has no filename.");

                    var folder = "";
                    var dict = new Dictionary<string, string>();
                    if (tex.Filename != null)
                    {
                        folder = Path.GetDirectoryName(tex.Filename);
                        dict.Add("Source", tex.Filename);
                    }
                    else
                    {
                        folder = Path.GetDirectoryName(tex.MultiFilenames[0]);

                        for (var i = 0; i < tex.MultiFilenames!.Length; i++)
                        {
                            dict.Add($"$face:{i}", tex.MultiFilenames[i]);
                        }
                    }

                    var record = new TextureRecord
                    {
                        GId = newGid,
                        Name = tex.Name,
                        Files = dict,
                        Preset = tex.Preset,
                        PixelFormat = tex.PixelFormat,
                        Anisotropy = tex.Anisotropy,
                        LodBias = tex.LodBias,
                        InMemory = tex.InMemory,
                        TextureKind = dict.Count == 1 ? TextureKind.Texture2D : TextureKind.CubeMap
                    };

                    return (record, Path.Combine(EnginePath.TexturePath, folder, tex.Name));
                }

            case MeshDescriptor mesh:
                {
                    var record = new ModelRecord
                    {
                        GId = newGid,
                        Name = mesh.Name,
                        Files = { { "Main", mesh.Filename } },
                        SubMeshCount = 0,
                        HasAnimation = false
                    };
                    return (record, Path.Combine(EnginePath.MeshPath, mesh.Name));
                }

            case ShaderDescriptor shader:
                {
                    var record = new ShaderRecord
                    {
                        GId = newGid,
                        Name = shader.Name,
                        Files =
                        {
                            { ShaderRecord.VertexFileKey, shader.VertexFilename },
                            { ShaderRecord.FragmentFileKey, shader.FragmentFilename }
                        },
                    };

                    var savePath = Path.Combine(EnginePath.ShaderPath, shader.Name);
                    return (record, savePath);
                }

            case MaterialDescriptor mat:
                {
                    var record = new MaterialRecord
                    {
                        GId = newGid,
                        Name = mat.Name,
                        Shader = mat.Shader,
                        DepthWrite = mat.DepthWrite,
                        ReceiveShadows = mat.ReceiveShadows,
                        CastShadows = mat.CastShadows,
                        Profile = mat.Profile,
                        ProfileSlots = mat.ProfileSlots,
                        Parameters = mat.Parameters,
                        TextureSlots = mat.TextureSlots
                    };

                    var savePath = Path.Combine(EnginePath.MaterialPath, mat.Name);
                    return (record, savePath);
                }

            default:
                throw new NotImplementedException($"No migration path for {desc.GetType().Name}");
        }
    }
}*/